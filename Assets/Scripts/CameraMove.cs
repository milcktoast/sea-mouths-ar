using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour {
	public float angleRangeX = 120f;
	public float angleRangeY = 120f;

	private Camera activeCamera;
	private Vector3 rotationTarget = new Vector3(0f, 0f, 0f);

	void Start () {
		activeCamera = GetComponent<Camera>();
	}

	void Update () {
		float dx = Mathf.Lerp(-1, 1, Input.mousePosition.x / Screen.width);
		float dy = Mathf.Lerp(1, -1, Input.mousePosition.y / Screen.height);

		rotationTarget.x = dy * angleRangeX;
		rotationTarget.y = dx * angleRangeY;

		activeCamera.transform.localEulerAngles = rotationTarget;
	}
}
