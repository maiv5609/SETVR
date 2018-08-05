using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Questions : MonoBehaviour {
	//Metrics
	private string filePath;
	private int pauses; 
	private StreamWriter writer;
	private Stopwatch timeline = new Stopwatch(); // Used for getting timestamps 
	private Stopwatch responseLength = new Stopwatch(); //Times 

	//Current indexes for lists
	private int index1 = 0;
	private int index2 = 0;
	private int index3 = 0;
	//Lists to store questions
	private List<string> phase1 = new List<string>();
	private List<string> phase2 = new List<string>();
	private List<string> phase3 = new List<string>();
	private bool endQuestions = false;

	//Audioclip for interviewer responses
	private int voiceSampleSize;
	private AudioClip response;

	//User Mic Input
	public float sensitivity;
	private int silenceTime;
	private int interruptCounter = 0;
	private Stopwatch stopwatch = new Stopwatch (); //used to detect slience
	private AudioClip microphoneInput;
	private bool userAnswered = false;
	private bool interrupted = false;

	//Recording Clips
	private AudioClip currentClip;
	private bool clipRecorded = false;
	private int clipCounter = 0;

	// Called when object has been initialized by Unity
	void Awake () {
		filePath = Application.dataPath + "metrics.csv";
		writer = new StreamWriter (filePath);
		timeline.Start ();
		/* Reading in Questions */
		string line;
		string path;
		path = Application.dataPath;
		silenceTime = 5;
		
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

		//Set detection variables to true to start detection, they will be reset later
		userAnswered = true;
		clipRecorded = true;
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
					print ("interrupt");
					interrupted = true;
				} else if (GameObject.Find ("One shot audio") == null && !userAnswered && !clipRecorded) {
					//If user hasn't answered, audio clip hasn't been recorded yet
					//This should only be used once per question to begin the recording
					print ("Recording clip");
					recordAudioClip();
				} 

				//print ("Level: " + level);
			} else {
				//User is not speaking
				if (userAnswered && GameObject.Find ("One shot audio") == null) {
					//Audio has been recorded and User has answered the question 
					//Check for silence
					stopwatch.Start();

					waveData = new float[1000];
					micPosition = Microphone.GetPosition (null) - (1000 + 1); // null means the first microphone
					microphoneInput.GetData (waveData, micPosition);
					//Check if user has not said anything during the last couple seconds before moving on
					levelMax = 0;
					wavePeak = 0;
					for (int i = 0; i < 1000; i++) {
						wavePeak = waveData [i] * waveData [i];
						if (levelMax < wavePeak) {
							levelMax = wavePeak;
						}
					}

					level = Mathf.Sqrt (Mathf.Sqrt (levelMax));
					TimeSpan ts = stopwatch.Elapsed;
					//print ("Total time of silence " + ts.TotalSeconds);

					if (level > sensitivity) {
						stopwatch.Reset ();
					} else {
						//If x seconds of silence has passed, continue
						if (!clipRecorded && userAnswered && ts.TotalSeconds >= silenceTime) {
							stopAudioClip ();
							nextQuestion ();
						}
						if (3 >= silenceTime) {
							pauses++;
						}
					}
				}
			}
		}
	}

	/* Saves timestamp of answered question to csv
	 * 
	 */
	void saveTimestamp (int phase, int question, int minute, int second){
		writer.WriteLine ("phase, question, minute, second");
		writer.WriteLine (phase.ToString() + "," + question.ToString() + "," + minute.ToString() + "," + second.ToString());
		writer.Flush ();
	}

	/* Handles recording audioclip when called and detects if user is still speaking
	 * 
	 */
	public void recordAudioClip(){
		if (!userAnswered) {
			//Maximum clip length of 3 minutes
			//currentClip = Microphone.Start (Microphone.devices [0], true, 180, 44100);
			userAnswered = true;
			print ("userAnswered = true");
			responseLength.Start ();
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
		print ("clipRecorded = true");
		responseLength.Stop ();
		TimeSpan responseTimespan = responseLength.Elapsed;
		responseLength.Reset();
		writer.WriteLine ("responseLength");
		writer.WriteLine (responseTimespan.TotalMinutes.ToString(), responseTimespan.TotalSeconds.ToString());
		//SavWav.Save ("QuestionAudioClip" + clipCounter, currentClip);
	}



	/* Handles moving to each phase and question when needed
	 * 
	 */
	public void nextQuestion(){
		int currIndex = 0;

		if (interrupted) {
			interruptCounter++;
			print ("User Interrupted: " + interruptCounter);
			interrupted = false;
		}
			
		if (!endQuestions && userAnswered && clipRecorded) {
			print ("Next Question");
			stopwatch.Reset ();
			//Reset detection flags
			clipRecorded = false;
			userAnswered = false;
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
				endQuestions = true;
			}
		}
	}

	/* Takes in current interview phase and selected question
	 * Plays selected audio clip for question
	 */
	void InterviewerSpeak(int phase, int question){
		//TODO: Test out pausing audio clip in response to the user speaking, track number of pauses.
		//Explore alternative responses to the user talking while the interviewer is speaking.
		response = Resources.Load<AudioClip>("QuestionAudio/P" + phase+ "Q" + question);
		
		//Record timestamp to csv
		TimeSpan time = timeline.Elapsed;
		saveTimestamp (phase, question, (int)time.TotalMinutes, (int)time.TotalSeconds); 
		//void saveTimestamp (int phase, int question, int minute, int second)
		//AudioSource.PlayClipAtPoint (response, new Vector3(16, 1, 11));
		AudioSource.PlayClipAtPoint (response, new Vector3(16, 1, 11), 0.1f);
	}

	void OnApplicationQuit(){
		timeline.Stop ();
		if (Microphone.devices.Length != 0) {
			//Save Audio to file

			//TODO: uncomment this in final build
			//SavWav.Save ("microphoneInputTest", microphoneInput);
		}
		print ("Total Questions Interrupted: " + interruptCounter);
		//This closes the file
		writer.Close ();
	}
}
