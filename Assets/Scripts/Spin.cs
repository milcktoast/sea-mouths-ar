using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour {
	public float speed = 0.5f;
	private Vector3 rotation;

	void Start () {
		rotation = new Vector3();
	}

	void Update () {
		float t = Time.fixedTime;
		rotation.x = t * speed * 0.5f * 360;
		rotation.y = t * speed * 360;
		transform.eulerAngles = rotation;
	}
}
