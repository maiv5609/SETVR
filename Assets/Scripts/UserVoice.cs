using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserVoice : MonoBehaviour {

	private Microphone mic;
	public int voiceSampleSize;
	public float sensitivity;
	AudioClip microphoneInput;

	//TODO: Recording working, Need to figure out restricting to clips

	// Use this for initialization
	void Awake () {

		AudioSource audio = GetComponent<AudioSource> ();
		sensitivity = 0.3f; // Sensitivity for mic input

		if (Microphone.devices.Length == 0) {
			//pause and ask user to plug in mic
			print("no microphone plugged in");
		} else {
			microphoneInput = Microphone.Start(Microphone.devices[0], true, 999, 44100);


		}
	}
	
	// Update is called once per frame
	void Update () {

		//Get mic volume
		int dec = 128;
		float[] waveData = new float[dec];
		int micPosition = Microphone.GetPosition(null) - (dec + 1); // null means the first microphone
		microphoneInput.GetData(waveData, micPosition);

		//Getting a peak from the last 128 samples
		float levelMax = 0;
		float wavePeak;
		for (int i = 0; i < dec; i++) {
			wavePeak = waveData [i] * waveData [i];
			if (levelMax < wavePeak) {
				levelMax = wavePeak;
			}
		}

		float level = Mathf.Sqrt (Mathf.Sqrt (levelMax));

		//Mic input is louder than set sensitivity 
		if (level > sensitivity) {
			Debug.Log ("Mic input detected");
			Debug.Log ("Level: " + level);
		}

			
	}

	void OnApplicationQuit(){
		//Save Audio to file
		SavWav.Save ("microphoneInputTest", microphoneInput);
	}

	//takes volume data from microphone, averages it
	//returns average voice volume
	float voiceVolume(){

		AudioSource audio = GetComponent<AudioSource> ();
		float sum = 0;
		float voiceVolume = 0;
		float[] voiceData = new float[voiceSampleSize];

		if (voiceData != null) {
			audio.clip.GetData (voiceData, 0);
			for (int i = 0; i < voiceData.Length; i++) {
				sum += voiceData [i];
			}
			voiceVolume = sum / voiceSampleSize;
		}

		return voiceVolume;
	}
		
}
