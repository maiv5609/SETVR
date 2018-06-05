using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gaze : MonoBehaviour {

	public float lookAwayTime;
	private bool prevView = false;
	private float currTime;
	private float prevTime;
	private float attentionTime;
	private float distractionTime;
	private float startAttention;
	private float startDistracted;

	void Awake(){

		currTime = Time.time;
		prevTime = Time.time;
	}

	void OnDestroy(){

		print ("Attention Time: " + attentionTime);
		print ("Distrated Time: " + distractionTime);
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

			attentionTime += currTime - startDistracted;
			startAttention = Time.time;
			print (payingAttention);
			prevView = true;
		}

		if (prevView && !payingAttention) {

			distractionTime += currTime - startAttention;
			startDistracted = Time.time;
			print (payingAttention);
			prevView = false;
		}

		//has user been looking away for longer than attentionTime seconds
		if (currTime - prevTime >= lookAwayTime) {

			//trigger interviewer to acknowledge that the user is not paying attention
			print("User looked away for too long.");
		}

	}


}
