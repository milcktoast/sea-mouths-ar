using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleManager : MonoBehaviour {
	public GameObject holeGeneratorPrefab;


	void Start () {
		
	}
	
	public void GenerateHole (Vector3[] points) {
		GameObject instance = Instantiate(holeGeneratorPrefab);
		HoleGenerator generator = instance.GetComponent<HoleGenerator>();
		instance.transform.SetParent(transform);
		generator.GenerateFromPoints(points);
	}

	void Update () {
		
	}
}
