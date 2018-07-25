//TODO: Error with GetData had to do with me closing the microphone when making clips
// Currently working on detecting prolonged silence. There is an issue with questions continuely moving on 
// when the mic is on.
// Need to go into NextQUestion and add a gate (until the user has answered)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Questions : MonoBehaviour {
	//Current indexes for lists
	int index1 = 0;
	int index2 = 0;
	int index3 = 0;
	//Lists to store questions
	List<string> phase1 = new List<string>();
	List<string> phase2 = new List<string>();
	List<string> phase3 = new List<string>();

	//Audioclip for interviewer responses
	public int voiceSampleSize;
	bool interviewerSpeak = false; //Might not need this
	AudioClip response;

	//User Mic Input
	public float sensitivity;
	int interruptCounter = 0;
	AudioClip microphoneInput;
	bool userAnswered = false;
	bool interrupted = false;

	//Recording Clips
	AudioClip currentClip;
	bool clipRecorded = false;
	int clipCounter = 0;

	// Called when object has been initialized by Unity
	void Awake () {
		/* Reading in Questions */
		string line;
		string path;
		path = Application.dataPath;
		
		//Read in phase 1 questions
		System.IO.StreamReader file = new System.IO.StreamReader(path + "/Resources/Questions/Phase1.txt");
		while((line = file.ReadLine()) != null){
			phase1.Add(line);
		}
		//Read in phase 2 questions
		file = new System.IO.StreamReader(path + "/Resources/Questions/Phase2.txt");
		while((line = file.ReadLine()) != null){
			phase2.Add(line);
		}
		//Read in phase 3 questions
		file = new System.IO.StreamReader(path + "/Resources/Questions/Phase3.txt");
		while((line = file.ReadLine()) != null){
			phase3.Add(line);
		}	

		/* Setup mic detection for user */
		sensitivity = 0.3f; // Sensitivity for mic input

		if (Microphone.devices.Length == 0) {
			//pause and ask user to plug in mic
			print("no microphone plugged in");
		} else {
			microphoneInput = Microphone.Start(Microphone.devices[0], true, 1200, 44100);
		}
			
		/* Setting up event listening for user response */
		//EventManager.StartListening("interrupt", EventTest);
		//EventManager.TriggerEvent ("Answered");
		Invoke ("nextQuestion", 4); 
	}

	//TODO: Getting an error when accessing microphone, it seems like the functionality is not intially affected but seems to crash later

	// Update is called once per frame
	void Update () {
		if (Microphone.devices.Length != 0) {
			//Get mic volume
			int dec = 128;
			float[] waveData = new float[dec];
			int micPosition = Microphone.GetPosition (null) - (dec + 1); // null means the first microphone
			microphoneInput.GetData (waveData, micPosition);

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
				//Check if interviewer is talking (find the temp audiosource gameobject)
				if (GameObject.Find ("One shot audio") && !interrupted) {
					//User interrupted interviewer
					Debug.Log("interrupt");
					interrupted = true;
				} else if (!GameObject.Find ("One shot audio") && !userAnswered && !clipRecorded) {
					//If user hasn't answered, audio clip hasn't been recorded yet
					//This should only be used once per question to begin the recording
					Debug.Log("Recording clip");
					recordAudioClip();
				} 

				//Debug.Log ("Level: " + level);
			} else {
				//User is not speaking
				if (userAnswered && !GameObject.Find ("One shot audio")) {
					//Audio has been recorded and User has answered the question 
					//Check for silence
					waveData = new float[500];
					micPosition = Microphone.GetPosition (null) - (500 + 1); // null means the first microphone
					microphoneInput.GetData (waveData, micPosition);
					//Check if user has not said anything during the last couple seconds before moving on
					levelMax = 0;
					wavePeak = 0;
					for (int i = 0; i < 500; i++) {
						wavePeak = waveData [i] * waveData [i];
						if (levelMax < wavePeak) {
							levelMax = wavePeak;
						}
					}
					level = Mathf.Sqrt (Mathf.Sqrt (levelMax));

					if (level < sensitivity) {
						Debug.Log ("Silence Detected");
						if (!clipRecorded) {
							stopAudioClip ();
							Debug.Log ("Next Question");
							nextQuestion();
						}
					}
				}
			}
		}
	}

	/* Handles recording audioclip when called and detects if user is still speaking
	 * 
	 */
	public void recordAudioClip(){
		if (!userAnswered) {
			//Maximum clip length of 3 minutes
			//currentClip = Microphone.Start (Microphone.devices [0], true, 180, 44100);
			userAnswered = true;
			Debug.Log ("userAnswered = true");
		} 
	}

	/* Stops currently recording clip and flips flag
	 * 
	 */
	public void stopAudioClip(){
		clipRecorded = true;
		clipCounter++;
		//Microphone.End(Microphone.devices[0]);
		//Save Audio to file
		Debug.Log("clipRecorded = true");
		//SavWav.Save ("QuestionAudioClip" + clipCounter, currentClip);
	}


	/* Handles moving to each phase and question when needed
	 * 
	 */
	public void nextQuestion(){
		int currIndex = 0;
		//Reset detection flags
		clipRecorded = false;
		userAnswered = false;

		if (interrupted) {
			interruptCounter++;
			Debug.Log ("User Interrupted: " + interruptCounter);
			interrupted = false;
		}

		System.Random genRand = new System.Random();
		//phase 1 questions
		if (index1 == 0){
			GameObject.Find("QuestionText").GetComponent<Text>().text = " Current Question: " + phase1[0];
			InterviewerSpeak(1, 1);
			phase1.RemoveAt(0);
			index1++;
		}else if(index2 == 0 && index3 == 0){
			//TODO: remove $$ index2 == 0 when we want more questions from random pool
			//Check that we aren't in phase 3 
			//Select random index from phase2
			currIndex = genRand.Next(0, phase2.Count-1);
			GameObject.Find("QuestionText").GetComponent<Text>().text = " Current Question: " + phase2[currIndex];
			phase2.RemoveAt(currIndex);
			//TODO: remove this
			InterviewerSpeak(2, 1);
			index2++;
		}else if(index3 == 0){
			//Phase 3
			GameObject.Find("QuestionText").GetComponent<Text>().text = " Current Question: " + phase3[0];
			phase3.RemoveAt(0);
			InterviewerSpeak(2, 1);
			index3++;
		}else{
			GameObject.Find("QuestionText").GetComponent<Text>().text = "End of Questions";
		}
	}

	/* Takes in current interview phase and selected question
	 * Plays selected audio clip for question
	 */
	void InterviewerSpeak(int phase, int question){
		//TODO: Test out pausing audio clip in response to the user speaking, track number of pauses.
		//Explore alternative responses to the user talking while the interviewer is speaking.
		response = Resources.Load<AudioClip>("QuestionAudio/P" + phase+ "Q" + question);
		//AudioSource.PlayClipAtPoint (response, new Vector3(16, 1, 11));
		AudioSource.PlayClipAtPoint (response, new Vector3(16, 1, 11), 0.1f);
	}

	void OnApplicationQuit(){
		if (Microphone.devices.Length != 0) {
			//Save Audio to file
			SavWav.Save ("microphoneInputTest", microphoneInput);
		}
		Debug.Log("Total Questions Interrupted: " + interruptCounter);
	}
}
