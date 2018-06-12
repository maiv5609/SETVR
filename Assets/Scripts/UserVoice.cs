using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserVoice : MonoBehaviour {

	private Microphone mic;
	public int voiceSampleSize;
	public float isSpeakingThreshold;

	// Use this for initialization
	void Awake () {

		string[] devices = Microphone.devices;
		AudioSource audio = GetComponent<AudioSource> ();

		if (devices.Length == 0) {

			//pause and ask user to plug in mic
			print("no microphone plugged in");
		} else {
			
			audio.clip = Microphone.Start(devices [0], true, 1200, 16000);
			audio.loop = true;
			while (!(Microphone.GetPosition (null) > 0)) {}
			audio.Play ();
		}
	}
	
	// Update is called once per frame
	void Update () {
		
		float currVoiceVolume = voiceVolume();

		if (currVoiceVolume >= isSpeakingThreshold) {

			//do something cause the user spoke
			print("user is speaking");
		}
	}

	//takes volume data from microphone, averages it
	//returns average voice volume
	float voiceVolume(){

		AudioSource audio = GetComponent<AudioSource> ();
		float sum = 0;
		float voiceVolume = 0;
		float[] voiceData = new float[voiceSampleSize];
		audio.clip.GetData (voiceData, 0);

		for (int i = 0; i < voiceData.Length; i++) {

			sum += voiceData [i];
		}

		voiceVolume = sum / voiceSampleSize;

		return voiceVolume;
	}
}
