using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Analytics;

public class Gaze : MonoBehaviour {

	public float lookAwayTime; //Max time user can look away before being notified
	private bool prevView = false;
	private float currTime;
	private float prevTime;
	private float attentionTime;
	private float distractionTime;
	private float startAttention;
	private float startDistracted;
	float currCountdownValue;

	//Variables for fading UI based on eye contact
	public Image Icon; //Attach an image you want to fade in the GameObject's Inspector
	bool fading; //Use this to tell if the toggle returns true or false

	void Awake(){
		lookAwayTime = 4; 
		currTime = Time.time;
		prevTime = Time.time;
	}

	void OnDestroy(){
		print ("Attention Time: " + attentionTime);
		print ("Distracted Time: " + distractionTime);
	}

	// Update is called once per frame
	void Update () {

		currTime = Time.time;

		int layerMask = 1 << 8;

		RaycastHit hit;
		bool payingAttention = Physics.Raycast (transform.position, transform.forward, Mathf.Infinity, layerMask);
		Debug.DrawRay (transform.position, transform.forward * 100f, Color.red);

		//Making Eye Contact
		if (payingAttention) {
			changeFade (false);
		}

		//Making Eye Contact, tally
		if (!prevView && payingAttention) {
			attentionTime += currTime - startDistracted;
			startAttention = Time.time;
			prevView = true;
		}

		//Breaking Eye Contact
		if (prevView && !payingAttention) {
			distractionTime += currTime - startAttention;
			startDistracted = Time.time;
			prevView = false;
			StartCoroutine (StartCountdown ());
		}
	}

	//Countdown to track time spent not making eye contact
	public IEnumerator StartCountdown(float countdownValue = 2)
	{
		currCountdownValue = countdownValue;
		while (currCountdownValue > 0)
		{
			Debug.Log("Countdown: " + currCountdownValue);
			yield return new WaitForSeconds(1.0f);
			currCountdownValue--;
		}
		if (currCountdownValue == 0) {
			changeFade (true);
		}
	}

	//Adjusts fade options
	public void changeFade(bool fade){
		//If fading is true, fade in Gaze Indicator
		if (fade == true) {
			Icon.CrossFadeAlpha(1, 2.0f, false);
		}
		//If fading is false, fade out Gaze Indicator
		else if (fade == false) {
			Icon.CrossFadeAlpha(0, 0.5f, false);
		}
	}
}
