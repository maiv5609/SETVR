﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

//TODO: put all csv formats in one place, Take the minute values and floor them
/* Added: 
 * Detection for speaking too loudly or softly
 * Interacting with objects
 * Breaking eye contact for too long
 * Interrupts
 * Pauses
 */

public class Questions : MonoBehaviour {
	//Metrics
	private string filePath;
	private int pauses;
    private int objectsGrabbed;
	private Stopwatch timeline = new Stopwatch(); // Used for getting timestamps 
	private Stopwatch responseLength = new Stopwatch(); //Times 

    //Writers for csv files
    private StreamWriter questionResponses;
    private StreamWriter responseLengths;
    private StreamWriter alerts;
    private StreamWriter miscMetrics;

    //Current indexes for lists
    private int index1 = 0;
	private int index2 = 0;
	private int index3 = 0;
	//Lists to store questions
	private List<string> phase1 = new List<string>();
	private List<string> phase2 = new List<string>();
	private List<string> phase3 = new List<string>();
	private bool endQuestions = false;
    private int currQuestion = 1;

    //Audioclip for interviewer responses
    private int voiceSampleSize;
	private AudioClip response;

	//User Mic Input
	public float sensitivity; //Used for detecting talking in general, default 0.1
    public float upperSensitivity; //Upper bound on volume, default .6 or .7
    public float lowerSensitivity; //Lower bound on volume, default .3
    private int silenceTime;
	private int interruptCounter = 0;
	private Stopwatch silence = new Stopwatch (); //used to detect slience
    private Stopwatch volume = new Stopwatch(); //used to time volume peak time
    private AudioClip microphoneInput;
	private bool userAnswered = false;
	private bool interrupted = false;
    private bool pauseLock = false;

	//Recording Clips
	private AudioClip currentClip;
	private bool clipRecorded = false;
	private int clipCounter = 0;

    public Image loud;
    private Stopwatch loudWatch = new Stopwatch();


    // Called when object has been initialized by Unity
    void Awake () {
        //Inital UI
        loud.CrossFadeAlpha(0, 0.1f, false);

        //Create csv files for metrics
        questionResponses = new StreamWriter (Application.dataPath + "/Resources/Visualizations/CSV/responseTimestamps.csv");
        responseLengths = new StreamWriter(Application.dataPath + "/Resources/Visualizations/CSV/responseLengths.csv");
        alerts = new StreamWriter(Application.dataPath + "/Resources/Visualizations/CSV/alerts.csv");
        miscMetrics = new StreamWriter(Application.dataPath + "/Resources/Visualizations/CSV/miscMetrics.csv");

        timeline.Start ();
		/* Reading in Questions */
		string line;
		string path;
		path = Application.dataPath;
		silenceTime = 5;
		
        /* This will be used for UI in the random pool questions, but will not be needed for the study
         * 
         * 
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
        *
        * 
        */

		/* Setup mic detection for user */
		if (Microphone.devices.Length == 0) {
			//pause and ask user to plug in mic
			print("no microphone plugged in");
		} else {
			microphoneInput = Microphone.Start(Microphone.devices[0], true, 1200, 44100);
		}

		//Set detection variables to true to start detection, they will be reset later
		userAnswered = true;
		clipRecorded = true;
		Invoke ("nextQuestion", 4); 
	}

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

            if (level > sensitivity) {
                if (level > upperSensitivity) {
                    //User is speaking too loudly
                    if (!loudWatch.IsRunning){
                        print("Loud start");
                        AddAlert("High");
                        loudWatch.Start();
                        loud.CrossFadeAlpha(1, 0.5f, false);
                    }
                }

                //Check if interviewer is talking (find the temp audiosource gameobject)
                if (GameObject.Find ("One shot audio") && !interrupted) {
					//User interrupted interviewer
					print ("interrupt");
					interrupted = true;
                    AddAlert("Interrupt");
				} else if (GameObject.Find ("One shot audio") == null && !userAnswered && !clipRecorded) {
					//If user hasn't answered, audio clip hasn't been recorded yet
					//This should only be used once per question to begin the recording
					print ("Recording clip");
					recordAudioClip();
				} 

				//print ("Level: " + level);
			} else {
                //Check UI timers
                if (loudWatch.IsRunning && loudWatch.Elapsed.Seconds > 3){
                    print("Loud end");
                    loud.CrossFadeAlpha(0, 0.5f, false);
                    loudWatch.Reset();
                    loudWatch.Stop();
                }
                //User is not speaking
                if (userAnswered && GameObject.Find ("One shot audio") == null) {
					//Audio has been recorded and User has answered the question 
					//Check for silence
					silence.Start();

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
					TimeSpan ts = silence.Elapsed;

					if (level > sensitivity) {
                        if (!pauseLock && (2 <= ts.Seconds && 5 > ts.Seconds))  {
                            pauses++;
                            pauseLock = true;
                            print("Silence Time: " + ts.TotalSeconds);
                            AddAlert("Pause");
                        }
                        silence.Reset ();
					} else {
                        if (1 <= ts.Seconds && pauseLock){
                            pauseLock = false;
                            print("Pause unlocked");
                        }
                        //If x seconds of silence has passed, continue
                        if (!clipRecorded && userAnswered && ts.Seconds >= silenceTime) {
                            pauseLock = false;
                            stopAudioClip ();
							nextQuestion ();
						}
						
					}
				}
			}
		}
	}

	/* Saves timestamp of answered question to csv
	 * Format of csv: minute, second
	 */
	void questionTimestamp (int minute, int second){
        questionResponses.WriteLine (minute.ToString() + "," + second.ToString());
        questionResponses.Flush ();
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
	 * Format of csv: minute, second
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
        responseLengths.WriteLine (responseTimespan.Minutes.ToString(), responseTimespan.Seconds.ToString());
		//SavWav.Save ("QuestionAudioClip" + clipCounter, currentClip);
	}



	/* Handles moving to each phase and question when needed
	 * 
	 */
	public void nextQuestion(){
		if (interrupted) {
			interruptCounter++;
			print ("User Interrupted: " + interruptCounter);
			interrupted = false;
		}
			
		if (!endQuestions && userAnswered && clipRecorded) {
			print ("Next Question");
			silence.Reset ();
			//Reset detection flags
			clipRecorded = false;
			userAnswered = false;

            InterviewerSpeak(currQuestion);
            currQuestion++;

            if(currQuestion == 9) {
                endQuestions = true;
            }

            /* This Section if for the randomized pool of questions, for inital study there will be a structured path of questions
             * 
             * 
            System.Random genRand = new System.Random();
			if (index1 == 0){
                //phase 1 questions
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
            *
            *
            */
        }
    }

	/* Takes in current interview phase and selected question
	 * Plays selected audio clip for question
     * Currently adjusted to have a structured series of questions
	 */
	void InterviewerSpeak(int question){
		response = Resources.Load<AudioClip>("QuestionAudio/Q" + question);
        print("Question: " + question);
		//Record timestamp to csv
		TimeSpan time = timeline.Elapsed;
		questionTimestamp ((int)time.TotalMinutes, (int)time.TotalSeconds); 
		AudioSource.PlayClipAtPoint (response, new Vector3(16, 1, 11), 0.1f);
	}

    public void AddAlert(string alertType){
        if(alertType == "Grab"){
            objectsGrabbed++;
        }
        TimeSpan time = timeline.Elapsed;
        alerts.WriteLine(time.Minutes + "," + time.Seconds + "," + alertType);
    }

    //TODO
	void OnApplicationQuit(){
        miscMetrics.WriteLine("Interrupts, "+ interruptCounter);
        miscMetrics.WriteLine("Pauses, " + pauses);
        miscMetrics.WriteLine("Objects grabbed, " + objectsGrabbed);
        miscMetrics.Flush();

        //TODO: will probably have to move this to a seperate function that closes all files, Call this when we move scenes
        //Closes the file
        questionResponses.Close();
        responseLengths.Close();
        alerts.Close();
        miscMetrics.Close ();

        timeline.Stop();
        if (Microphone.devices.Length != 0)
        {
            //Save Audio to file

            //TODO: uncomment this in final build
            SavWav.Save("userAudio", microphoneInput);
        }
    }
}
