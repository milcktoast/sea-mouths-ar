using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour {
	public HoleManager holeManager;
	public GameObject activePointPrefab;
	public float baseScale = 0.02f;

	private GameObject[] activePoints;
	private float[] activePointState;
	private float[] activePointStatePrev;
	private Vector3 focusPointPosition;
	private float focusPointDepth = 0.5f;
	private float focusPointDepthPrev = 0.25f;
	private int activePointsMax = 3;
	private int activePointsCount = 0;
	private bool hoverIsDisabled = false;

	void Start () {
		InstantiateActivePoints();
	}

	void InstantiateActivePoints() {
		int count = activePointsMax + 1;
		activePoints = new GameObject[count];
		activePointState = new float[count];
		activePointStatePrev = new float[count];
		focusPointPosition = new Vector3();

		for (int i = 0; i < count; i++) {
			GameObject instance = Instantiate(activePointPrefab);
			instance.SetActive(false);
			instance.transform.SetParent(transform);
			activePoints[i] = instance;
			activePointState[i] = 0f;
			activePointStatePrev[i] = 0f;
		}
	}

	public void FocusPoint (Vector3 point) {
		FocusPointInstance(0, point);
	}

	public void BlurPoint () {
		BlurPointInstance(0);
	}

	public void SelectPoint (Vector3 point) {
		bool shouldAppend = activePointsCount < activePointsMax &&
			!PointIsActive(point, 0.02f);
		
		if (shouldAppend) {
			activePointsCount++;
			SelectPointInstance(activePointsCount,
				activePoints[0].transform.localPosition);

			if (activePointsCount == activePointsMax) {
				CompleteSelection();
				StartCoroutine(AnimateBlurSelectedPoints());
			}
		}
	}

	void CompleteSelection () {
		Vector3[] positions = GetActivePointPositions();
		holeManager.GenerateHole(positions);
	}

	Vector3[] GetActivePointPositions () {
		Vector3[] positions = new Vector3[activePointsMax];
		for (int i = 0; i < activePointsMax; i++) {
			int index = i + 1;
			positions[i] = activePoints[index].transform.localPosition;
		}
		return positions;
	}

	void FocusPointInstance (int index, Vector3 position) {
		GameObject instance = activePoints[index];
		instance.SetActive(true);
		focusPointDepth = Vector3.Distance(
			Camera.main.transform.position, position);
		activePointState[index] = 1f;
	}

	void BlurPointInstance (int index) {
//		GameObject instance = activePoints[index];
//		instance.SetActive(false);
		focusPointDepth = 0.5f;
		activePointState[index] = 0f;
	}

	void SelectPointInstance (int index, Vector3 position) {
		GameObject instance = activePoints[index];
		instance.SetActive(true);
		instance.transform.localPosition = position;
		activePointState[index] = 1f;
	}

	bool PointIsActive (Vector3 point, float tolerance) {
		for (int i = 0; i < activePointsCount; i++) {
			int index = i + 1;
			Vector3 pointPosition = activePoints[index].transform.localPosition;
			float dist = Vector3.Distance(point, pointPosition);
			if (dist <= tolerance)
				return true;
		}
		return false;
	}

	IEnumerator AnimateBlurSelectedPoints () {
		hoverIsDisabled = true;
		for (int i = 0; i < activePointsCount; i++) {
			int index = i + 1;
			BlurPointInstance(index);
			yield return new WaitForSeconds(0.2f);
		}

		yield return new WaitForSeconds(2f);
		hoverIsDisabled = false;
		activePointsCount = 0;
	}

	void Update () {
		UpdatePointStates();
	}

	void UpdatePointStates () {
		if (hoverIsDisabled) {
			activePointState[0] = 0f;
			activePointStatePrev[0] = 0f;
		}

		float nextFocusPointDepth = focusPointDepthPrev + (focusPointDepth - focusPointDepthPrev) * 0.1f;
		focusPointPosition = Camera.main.transform.position +
			Camera.main.transform.forward.normalized * nextFocusPointDepth;
		focusPointDepthPrev = nextFocusPointDepth;
		activePoints[0].transform.localPosition = focusPointPosition;

		for (int i = 0; i < activePointsMax + 1; i++) {
			GameObject activePoint = activePoints[i];
			float currentState = activePointStatePrev[i];
			float targetState = activePointState[i];
			float nextState = currentState + (targetState - currentState) * 0.1f;

			float s = nextState * baseScale;
			activePoint.transform.localScale = new Vector3(s, s, s);

			activePointStatePrev[i] = nextState;
		}
	}
}
