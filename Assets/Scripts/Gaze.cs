using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Analytics;

public class Gaze : MonoBehaviour {

	public float lookAwayTime;
	private bool prevView = false;
	private float currTime;
	private float prevTime;
	private float attentionTime;
	private float distractionTime;
	private float startAttention;
	private float startDistracted;

	//Variables for fading UI based on eye contact
	public Image Icon; //Attach an image you want to fade in the GameObject's Inspector
	bool fading; //Use this to tell if the toggle returns true or false

	void Awake(){

		currTime = Time.time;
		prevTime = Time.time;
	}

	void OnDestroy(){

		print ("Attention Time: " + attentionTime);
		print ("Distracted Time: " + distractionTime);

		//Send information to Unity Analytics
		AnalyticsEvent.Custom ("Focus_Time", new Dictionary<string, object> {
			{"AttentionTime", attentionTime},
			{"DistractedTime", distractionTime}
		});
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
			prevView = true;
			fading = false;
		}

		if (prevView && !payingAttention) {

			distractionTime += currTime - startAttention;
			startDistracted = Time.time;
			prevView = false;
			fading = true;
		}

		//If fading is true, fade in Gaze Indicator
		if (fading == true) {
			Icon.CrossFadeAlpha(1, 2.0f, false);
		}
		//If fading is false, fade out Gaze Indicator
		else if (fading == false) {
			Icon.CrossFadeAlpha(0, 0.5f, false);
		}

		//has user been looking away for longer than attentionTime seconds
		if (currTime - prevTime >= lookAwayTime) {

			//trigger interviewer to acknowledge that the user is not paying attention
			print("User looked away for too long.");
		}

	}


}
