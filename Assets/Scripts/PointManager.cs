using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class PointManager : MonoBehaviour {
	public SelectionManager selectionManager;

	public int maxPointsToShow;
	public float particleSize = 1.0f;
	public bool generateDebugPoints = false;
	public Vector3 debugPointsRange = new Vector3(0.5f, 0.5f, 0f);

	private Vector3[] m_PointCloudData;
	private bool frameUpdated = false;
	private ParticleSystem currentPS;
	private ParticleSystem.Particle[] particles;
	private Vector3 selectRayOrigin;
	private float selectLastTime = 0f;

	void Start () {
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
		currentPS = GetComponent<ParticleSystem>();
		selectRayOrigin = new Vector3(Screen.width / 2f, Screen.height / 2f);
		frameUpdated = false;

		if (generateDebugPoints)
			GenerateTestPoints(maxPointsToShow, debugPointsRange);
	}

	void GenerateTestPoints (int count, Vector3 range) {
		Vector3[] pointData = new Vector3[count];
		for (int i = 0; i < count; i++) {
			pointData[i] = new Vector3(
				Random.Range(-range.x, range.x),
				Random.Range(-range.y, range.y),
				Random.Range(-range.z, range.z));
		}
			
		m_PointCloudData = pointData;
		frameUpdated = true;
	}

	public void ARFrameUpdated (UnityARCamera camera) {
		m_PointCloudData = camera.pointCloudData;
		frameUpdated = true;
	}

	void Update () {
		UpdatePoints();
		SelectPoints();
	}

	void UpdatePoints () {
		if (!frameUpdated)
			return;
		
		if (m_PointCloudData != null && m_PointCloudData.Length > 0) {
			int numParticles = Mathf.Min(m_PointCloudData.Length, maxPointsToShow);
			ParticleSystem.Particle[] particles = new ParticleSystem.Particle[numParticles];
			int index = 0;

			for (int i = 0; i < numParticles; i++) {
				particles[i].position = m_PointCloudData[i];
				particles[i].startColor = new Color (1.0f, 1.0f, 1.0f);
				particles[i].startSize = particleSize;
				index++;
			}

			currentPS.SetParticles (particles, numParticles);
		} else {
			ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1];
			particles [0].startSize = 0.0f;
			currentPS.SetParticles (particles, 1);
		}

		frameUpdated = false;
	}

	void SelectPoints () {
		if (m_PointCloudData != null && m_PointCloudData.Length > 1) {
			Ray ray = Camera.main.ScreenPointToRay(selectRayOrigin);
			Vector3 camPosition = Camera.main.transform.position;

			float maxDistance = 1f;
			float minCamDistance = 0.2f;
			float closestDistance = Mathf.Infinity;
			int closestIndex = -1;

			Vector3 closestPoint = new Vector3();
			int index = 0;

			foreach (Vector3 point in m_PointCloudData) {
				float dist = DistanceToLine(ray, point);
				float camDist = Vector3.Distance(camPosition, point);
				if (camDist > minCamDistance && dist < maxDistance && dist < closestDistance) {
					closestDistance = dist;
					closestIndex = index;
					closestPoint = point;
				}
				index++;
			}

			if (closestIndex != -1) {
				bool shouldSelect =
					(Time.fixedTime - selectLastTime > 1f) &&
					(Input.GetMouseButtonDown(0) || Input.touchCount > 0);
				if (shouldSelect) {
					selectionManager.SelectPoint(closestPoint);
					selectLastTime = Time.fixedTime;
				} else {
					selectionManager.FocusPoint(closestPoint);
				}
			} else {
				selectionManager.BlurPoint();
			}
		}
	}

	float DistanceToLine(Ray ray, Vector3 point) {
		return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
	}

}
