using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(MeshFilter))]
public class HoleGenerator : MonoBehaviour {
	public GameObject boundingItemPrefab;
	public GameObject tentacleItemPrefab;

	public int holeSliceCount = 11;
	public int holeSubDivisions = 3;
	public float baseScale = 0.05f;

	private bool isAnimating = false;
	private float ambientAnimationDuration = 0f;

	private Vector3 orientation;
	private GameObject tentaclesItem;

	private float openState = 0f;

	void Start () {
	}

	public void GenerateFromPoints (Vector3[] points) {
		Vector3 centroid = HoleGeometryUtility.CalculateCentroid(points);
		Vector3 normal = HoleGeometryUtility.CalculateNormal(points, Camera.main.transform.forward);
		Vector3[] localPoints = HoleGeometryUtility.LocalizePoints(points, centroid);
		int windingOrder = HoleGeometryUtility.CalculateWindingOrder(points, Camera.main.transform.forward);

		GenerateBaseMesh(normal, localPoints, windingOrder);
		GenerateBoundingMesh(normal, localPoints, windingOrder);
		GenerateTentaclesMesh(normal, localPoints);

		transform.localPosition = centroid;
		transform.localScale = new Vector3(0f, 0f, 0f);
		StartCoroutine(AnimateOpenTo(1f, 0.1f));
	}

	void GenerateBaseMesh (Vector3 normal, Vector3[] points, int windingOrder) {
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Material mat = GetComponent<MeshRenderer>().material;

		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uv = new List<Vector2>();
		List<int> triangles = new List<int>();

		int sliceCount = holeSliceCount;
		int sliceSubDivisions = holeSubDivisions;

		for (int i = 0; i < sliceCount; i++) {
			float indexf = (float)i;
			float countf = (float)sliceCount;

			float t = indexf / (countf - 1);
			float depth = indexf * 0.8f;
			float s0 = 1f - Mathf.Pow(1.3f * t - 0.3f, 2f);
			float scale = Mathf.Lerp(0f, 1f, s0) +
				((i % 2 == 0f) ? 0f : 0.2f);

			GenerateSlice(
				normal, points,
				vertices, normals, uv,
				sliceSubDivisions, scale, depth);
			
			if (i > 0) {
				GenerateSliceFaces(triangles,
					points.Length * (sliceSubDivisions + 1), sliceCount);
			}
		}

		mesh.vertices = vertices.ToArray();
		mesh.normals = normals.ToArray();
		mesh.uv = uv.ToArray();

		if (windingOrder == -1) {
			triangles.Reverse();
		}
		mesh.triangles = triangles.ToArray();

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		mat.renderQueue = 3020; // Render after bounding mask
		mat.SetColor("_Color", Color.HSVToRGB(
			Random.Range(0f, 1f), 0.9f, 0.8f));
	}

	void GenerateBoundingMesh (Vector3 normal, Vector3[] points, int windingOrder) {
		GameObject item = Instantiate(boundingItemPrefab, transform);
		Mesh mesh = new Mesh();

		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uv = new List<Vector2>();
		List<int> triangles = new List<int>();

		int sliceCount = holeSliceCount;
		int sliceSubDivisions = holeSubDivisions;
		float endDepth = (float)sliceCount * 0.8f;

		float[] scales = new float[] {
			1f, 1.2f, 1.2f, 0f};
		float[] depths = new float[] {
			0f, 0f, endDepth, endDepth};
		
		for (int i = 0; i < scales.Length; i++) {
			GenerateSlice(
				normal, points,
				vertices, normals, uv,
				sliceSubDivisions, scales[i], depths[i]);
		}

		GenerateSliceFaces(triangles,
			points.Length * (sliceSubDivisions + 1), scales.Length);

		mesh.vertices = vertices.ToArray();
		mesh.normals = normals.ToArray();
		mesh.uv = uv.ToArray();

		if (windingOrder == 1) {
			triangles.Reverse();
		}
		mesh.triangles = triangles.ToArray();

		item.GetComponent<MeshFilter>().mesh = mesh;
	}

//	TODO: Optimize
	void GenerateTentaclesMesh (Vector3 normal, Vector3[] points) {
		Mesh baseMesh = GetComponent<MeshFilter>().mesh;
		Mesh mesh = new Mesh();
		GameObject item = Instantiate(tentacleItemPrefab);

		int count = holeSubDivisions * 4;
		CombineInstance[] combine = new CombineInstance[count];

		for (int i = 0; i < count; i++) {
			Vector3 vPosition = baseMesh.vertices[i] * 1.2f;
			Vector3 vNormal = baseMesh.normals[i] * -1f;
			Quaternion rotation = Quaternion.LookRotation(vNormal, vPosition.normalized);

			GameObject instance = Instantiate(tentacleItemPrefab, vPosition, rotation);
			MeshFilter instanceMeshFilter = instance.GetComponent<MeshFilter>();

			combine[i].mesh = instanceMeshFilter.sharedMesh;
			combine[i].transform = instanceMeshFilter.transform.localToWorldMatrix;

			Destroy(instance);
		}

		mesh.CombineMeshes(combine);
		item.GetComponent<MeshFilter>().mesh = mesh;
		item.transform.SetParent(transform);
		item.transform.localScale = Vector3.one;
		tentaclesItem = item;
	}

	void GenerateSlice (
		Vector3 normal, Vector3[] points,
		List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv,
		int subDivisions, float scale, float depth
	) {
		int baseDivisions = points.Length;

		Vector3 sliceOffset = normal * depth * baseScale;
		Vector3 sliceCentroid = sliceOffset;

		for (int i = 0; i < baseDivisions; i++) {
			int indexA = i;
			int indexB = i < baseDivisions - 1 ? i + 1 : 0;
			Vector3 pointA = points[indexA] * scale + sliceOffset;
			Vector3 pointB = points[indexB] * scale + sliceOffset;

			vertices.Add(pointA);
			normals.Add(Vector3.zero);
			uv.Add(Vector2.zero);

			for (int j = 0; j < subDivisions; j++) {
				float jt = (float)(j + 1) / (float)(subDivisions + 1);
				Vector3 subPoint = Vector3.Lerp(pointA, pointB, jt);
				Vector3 subDir = subPoint - sliceCentroid;
				subDir.Normalize();
				subPoint += subDir * 1.8f * scale * baseScale;

				vertices.Add(subPoint);
				normals.Add(Vector3.zero);
				uv.Add(Vector2.zero);
			}
		}
	}

	void GenerateSliceFaces (
		List<int> triangles,
		int divisions, int count
	) {
		for (int i = 0; i < count - 1; i++) {
			TriangleGenerator.Rings(triangles,
				i * divisions, (i + 1) * divisions, divisions);
		}
	}

	void Update () {
		if (!isAnimating)
			UpdateAmbientAnimation();
	}

	void UpdateAmbientAnimation () {
		ambientAnimationDuration += Time.deltaTime;
		float t = ambientAnimationDuration;
		float s0 = Mathf.Lerp(0.8f, 1f, 
			Mathf.Sin(t * 0.8f + Mathf.PI * 0.5f) * 0.5f + 0.5f);
		float s1 = Mathf.Lerp(0.9f, 1f, 
			Mathf.Sin(t * 1.4f + Mathf.PI * 0.5f) * 0.5f + 0.5f);
		
		Vector3 scale = new Vector3(s0, s0, s0);
		transform.localScale = scale;

		Vector3 scaleTentacles = new Vector3(s1, s1, 1f);
		tentaclesItem.transform.localScale = scaleTentacles;
	}

	IEnumerator AnimateOpenTo (float target, float speedFactor) {
		isAnimating = true;
		yield return new WaitForSeconds(1f);
		Vector3 scale = new Vector3();

		while (Mathf.Abs(target - openState) > 0.001f) {
			openState += (target - openState) * speedFactor;

			float s = openState;
			scale.Set(s, s, s);
			transform.localScale = scale;

			yield return null;
		}

		scale.Set(target, target, target);
		transform.localScale = scale;
		isAnimating = false;
	}

	void OnDrawGizmos () {
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		if (mesh == null)
			return;

		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.color = Color.blue;
		foreach (Vector3 vertex in mesh.vertices) {
			Gizmos.DrawSphere(vertex, 0.1f * baseScale);
		}
	}
}


public static class HoleGeometryUtility {
	public static Vector3 CalculateCentroid (Vector3[] points) {
		Vector3 center = new Vector3();
		foreach (Vector3 point in points) {
			center += point;
		}
		center /= points.Length;
		return center;
	}

	public static Vector3 CalculateNormal (Vector3[] points, Vector3 forward) {
		Vector3 a = points[0];
		Vector3 b = points[1];
		Vector3 c = points[2];
		Vector3 normal = Vector3.Cross(b - a, c - a);
		float direction = Vector3.Dot(normal, a - forward);

		if (direction > 0f)
			normal *= -1f;

		normal.Normalize();
		return normal;
	}

	public static int CalculateWindingOrder (Vector3[] points, Vector3 forward) {
		Vector3 a = points[0];
		Vector3 b = points[1];
		Vector3 c = points[2];
		Vector3 normal = Vector3.Cross(b - a, c - a);
		float direction = Vector3.Dot(normal, a - forward);

		return (int)Mathf.Sign(direction);
	}

	public static Vector3[] LocalizePoints (Vector3[] points, Vector3 offset) {
		Vector3[] localPoints = new Vector3[points.Length];
		for (int i = 0; i < points.Length; i++) {
			localPoints[i] = points[i] - offset;
		}
		return localPoints;
	}
}


public static class TriangleGenerator {
	public static void Rings (
		List<int> triangles,
		int index0, int index1, int howMany
	) {
		int a, b, c, d;

		for (int i = 0; i < howMany - 1; i++) {
			a = index0 + i;
			b = index0 + i + 1;
			c = index1 + i + 1;
			d = index1 + i;

			triangles.AddRange(new int[6] {
				a, b, c, c, d, a});
		}

		a = index0 + howMany - 1;
		b = index0;
		c = index1;
		d = index1 + howMany - 1;

		triangles.AddRange(new int[6] {
			a, b, c, c, d, a});
	}
}
