using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleMove : MonoBehaviour {

	public float speed;
	public Vector3 endOfWorld;
	public Vector3 spawnPoint;
	
	// Update is called once per frame
	void Update () {

		Vector3 movement = new Vector3 (0, 0, speed * Time.deltaTime);
		transform.Translate (movement);

		if (transform.position.x >= endOfWorld.x) {

			transform.position = spawnPoint;
		}
	}
}
