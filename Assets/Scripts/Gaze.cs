using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gaze : MonoBehaviour {

	public float attentionTime;
	private bool prevView = false;
	private float currTime;
	private float prevTime;

	void Awake(){

		currTime = Time.time;
		prevTime = Time.time;
	}

	// Update is called once per frame
	void Update () {

		currTime = Time.time;

		int layerMask = 1 << 8;

		RaycastHit hit;
		bool payingAttention = Physics.Raycast (transform.position, transform.forward, Mathf.Infinity, layerMask);
		Debug.DrawRay (transform.position, transform.forward * 100f, Color.red);

		if (payingAttention) {

			prevTime = Time.time;
		}

		if (!prevView && payingAttention) {

			print (payingAttention);
			prevView = true;
		}

		if (prevView && !payingAttention) {

			print (payingAttention);
			prevView = false;
		}

		//has user been looking away for longer than attentionTime seconds
		if (currTime - prevTime >= attentionTime) {

			//trigger interviewer to acknowledge that the user is not paying attention
			print("User looked away for too long.");
		}

	}
}
