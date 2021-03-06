﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Questions : MonoBehaviour
{
    public Talk interviewer;
    public bool talking = false;

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
    public float sensitivity; //Used for detecting talking in general
    public float upperSensitivity; //Upper bound on volume
    public float lowerSensitivity; //Lower bound on volume
    private int silenceTime;
    private int interruptCounter = 0;
    private Stopwatch silence = new Stopwatch(); //used to detect slience
    private Stopwatch volume = new Stopwatch(); //used to time volume peak time
    private AudioClip microphoneInput;
    private bool userAnswered = false;
    private bool interrupted = false;
    private bool pauseLock = false;
    private bool shortLock = false; //Flag so that user is only alerted once per question for having a short response
    private bool longLock = false; //Flag so that user is only alerted once per question for having a long response

    //Recording Clips
    private AudioClip currentClip;
    private bool clipRecorded = false;
    private int clipCounter = 0;

    public Image loud;
    public Image shortImage;
    public Image longImage;

    private Stopwatch loudWatch = new Stopwatch();


    // Called when object has been initialized by Unity
    void Awake()
    {
        //Inital UI
        loud.CrossFadeAlpha(0, 0.1f, false);
        shortImage.CrossFadeAlpha(0, 0.1f, false);
        longImage.CrossFadeAlpha(0, 0.1f, false);

        //Create csv files for metrics
        questionResponses = new StreamWriter(Application.dataPath + "/Resources/Visualizations/responseTimestamps.csv");
        responseLengths = new StreamWriter(Application.dataPath + "/Resources/Visualizations/responseLengths.csv");
        alerts = new StreamWriter(Application.dataPath + "/Resources/Visualizations/alerts.csv");
        miscMetrics = new StreamWriter(Application.dataPath + "/Resources/Visualizations/miscMetrics.csv");

        //Formats for each of the csv's
        questionResponses.WriteLine("Question,Timestamp (Minutes)");
        responseLengths.WriteLine("Timestamp (Minutes),value,Minutes,Question");
        alerts.WriteLine("Alert,Timestamp (Minutes)");
        miscMetrics.WriteLine("Metric");

        timeline.Start();
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
        if (Microphone.devices.Length == 0)
        {
            print("no microphone plugged in");
        }
        else
        {
            microphoneInput = Microphone.Start(Microphone.devices[0], true, 1200, 44100);
        }

        //Set detection variables to true to start detection, they will be reset later
        userAnswered = true;
        clipRecorded = true;
        Invoke("nextQuestion", 4);
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameObject.Find("One shot audio") && talking)
        {
            interviewer.stopTalking();
            talking = false;
        }

        if (Microphone.devices.Length != 0)
        {
            //Get mic volume
            int dec = 128;
            float[] waveData = new float[dec];
            int micPosition = Microphone.GetPosition(null) - (dec + 1); // null means the first microphone
            microphoneInput.GetData(waveData, micPosition);

            //Getting a peak from the last 128 samples
            float levelMax = 0;
            float wavePeak;
            for (int i = 0; i < dec; i++)
            {
                wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }

            float level = Mathf.Sqrt(Mathf.Sqrt(levelMax));
            if(level >= 0.3)
            {
                //Debug statement mostly used for testing audio levels
                //print("Level: " + level);
            }

            if (level > sensitivity)
            {
                if (level > upperSensitivity)
                {
                    //User is speaking too loudly
                    if (!loudWatch.IsRunning)
                    {
                        print("Loud start");
                        AddAlert("Speaking Loud");
                        loudWatch.Start();
                        loud.CrossFadeAlpha(1, 0.5f, false);
                    }
                }

                //Check if interviewer is talking (find the temp audiosource gameobject)
                if (GameObject.Find("One shot audio") && !interrupted)
                {
                    //User interrupted interviewer
                    print("interrupt");
                    interrupted = true;
                    AddAlert("Interrupt");
                }
                else if (GameObject.Find("One shot audio") == null && !userAnswered && !clipRecorded)
                {
                    //If user hasn't answered, audio clip hasn't been recorded yet
                    //This should only be used once per question to begin the recording
                    print("Recording clip");
                    recordAudioClip();
                }
            }
            else
            {
                //Check UI timers
                if (loudWatch.IsRunning && loudWatch.Elapsed.Seconds > 3)
                {
                    print("Loud end");
                    loud.CrossFadeAlpha(0, 0.5f, false);
                    loudWatch.Reset();
                    loudWatch.Stop();
                }
                //User is not speaking
                if (GameObject.Find("One shot audio") == null)
                {
                    //Audio has been recorded and User has answered the question 
                    //Check for silence
                    silence.Start();
                    if (userAnswered)
                    {
                        waveData = new float[1000];
                        micPosition = Microphone.GetPosition(null) - (1000 + 1); // null means the first microphone
                        microphoneInput.GetData(waveData, micPosition);
                        //Check if user has not said anything during the last couple seconds before moving on
                        levelMax = 0;
                        wavePeak = 0;
                        for (int i = 0; i < 1000; i++)
                        {
                            wavePeak = waveData[i] * waveData[i];
                            if (levelMax < wavePeak)
                            {
                                levelMax = wavePeak;
                            }
                        }

                        level = Mathf.Sqrt(Mathf.Sqrt(levelMax));
                        TimeSpan ts = silence.Elapsed;

                        if (level > sensitivity)
                        {
                            if (!pauseLock && (2 <= ts.Seconds && 5 > ts.Seconds))
                            {
                                pauses++;
                                pauseLock = true;
                                print("Silence Time: " + ts.TotalSeconds);
                                AddAlert("Pause");
                            }
                            silence.Reset();
                        }
                        else
                        {
                            //Check response length if user has been silent for 2 seconds, only alert them once per question for either one
                            TimeSpan rs = responseLength.Elapsed;
                            if (ts.Seconds >= 2 && rs.Seconds > 0 && rs.Seconds <= 30 && !shortLock)
                            {
                                silenceTime = 10;
                                shortLock = true;
                                //Pop up icon saying to speak more
                                shortImage.CrossFadeAlpha(1, 0.1f, false);
                                print("Short Response");
                                AddAlert("Short Response");
                                Invoke("hideHourglass", 3);
                            }
                            else if (rs.Seconds > 30 && rs.Seconds < 120)
                            {
                                silenceTime = 5;
                                //Do nothing, continue
                            }
                            else if (ts.Seconds >= 2 && rs.Seconds > 120 && !longLock)
                            {
                                silenceTime = 2;
                                longLock = true;
                                //Pop up icon saying you spoke too much
                                longImage.CrossFadeAlpha(1, 0.1f, false);
                                print("Long Response");
                                AddAlert("Long Response");
                                Invoke("hideHourglass", 3);
                            }

                            if (1 <= ts.Seconds && pauseLock)
                            {
                                pauseLock = false;
                                print("Pause unlocked");
                            }
                            //If x seconds of silence has passed, continue
                            if (!clipRecorded && userAnswered && ts.Seconds >= silenceTime)
                            {
                                pauseLock = false;
                                stopAudioClip();
                                nextQuestion();
                            }
                        }
                    }
                    else if (!userAnswered)
                    {
                        TimeSpan ts = silence.Elapsed;
                        if (level > sensitivity)
                        {
                            if (pauseLock && 1 <= ts.Seconds && pauseLock)
                            {
                                pauseLock = false;
                                print("Pause unlocked");
                            }
                            else if (!pauseLock && (2 <= ts.Seconds && 5 > ts.Seconds))
                            {
                                pauses++;
                                pauseLock = true;
                                print("Silence Time: " + ts.TotalSeconds);
                                AddAlert("Pause");
                            }
                            silence.Reset();
                        }
                        else
                        {
                            if (!pauseLock && (2 <= ts.Seconds && 5 > ts.Seconds))
                            {
                                pauses++;
                                pauseLock = true;
                                print("Silence Time: " + ts.TotalSeconds);
                                AddAlert("Pause");
                            }
                        }
                    }
                }
            }
        }
    }


    void hideHourglass()
    {
        shortImage.CrossFadeAlpha(0, 0.1f, false);
        longImage.CrossFadeAlpha(0, 0.1f, false);
    }

    /* Saves timestamp of answered question to csv
	 * 
	 */
    void questionTimestamp(int minute, int second)
    {
        double temp = (double)minute + ((double)second / 60);
        temp = Math.Round(temp, 2);
        questionResponses.WriteLine(currQuestion + "," + temp);
        questionResponses.Flush();
    }

    /* Handles recording audioclip when called and detects if user is still speaking
	 * 
	 */
    public void recordAudioClip()
    {
        if (!userAnswered)
        {
            //Maximum clip length of 3 minutes
            //currentClip = Microphone.Start (Microphone.devices [0], true, 180, 44100);
            userAnswered = true;
            print("userAnswered = true");
            responseLength.Start();
        }
    }

    /* Stops currently recording clip and flips flag
     * We currently don't use this to actually stop an audioclip as we currently just record the entire session
     * This is currently used whenever the current question is finished
	 * Format of csv: minute, questionNumber
	 */
    public void stopAudioClip()
    {
        //Reset Flags
        clipRecorded = true;
        shortLock = false;
        longLock = false;
        clipCounter++;
        //Microphone.End(Microphone.devices[0]);
        //Save Audio to file
        print("clipRecorded = true");
        responseLength.Stop();
        TimeSpan timestamp = timeline.Elapsed; //Timestamp
        TimeSpan responseTimespan = responseLength.Elapsed; //Time spent talking
        responseLength.Reset();
        double stampTemp = (double)timestamp.Minutes + ((double)timestamp.Seconds / 60);
        stampTemp = Math.Round(stampTemp, 2);
        double timeTemp = (double)responseTimespan.Minutes + ((double)responseTimespan.Seconds / 60);
        timeTemp = Math.Round(timeTemp, 2);
        string slot = (currQuestion - 1).ToString();
        responseLengths.WriteLine(stampTemp + ",1," + timeTemp.ToString() + "," + slot);
        responseLengths.Flush();
    }



    /* Handles moving to each phase and question when needed
	 * 
	 */
    public void nextQuestion()
    {
        if (interrupted)
        {
            interruptCounter++;
            print("User Interrupted: " + interruptCounter);
            interrupted = false;
        }

        if (!endQuestions && userAnswered && clipRecorded)
        {
            print("Next Question");
            silence.Reset();
            //Reset detection flags
            clipRecorded = false;
            userAnswered = false;
            if (currQuestion == 9)
            {
                endQuestions = true;
                endSimulation();
            }
            else
            {
                InterviewerSpeak(currQuestion);
                currQuestion++;
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
    void InterviewerSpeak(int question)
    {
        response = Resources.Load<AudioClip>("QuestionAudio/Q" + question);
        interviewer.startTalking();
        talking = true;
        print("Question: " + question);
        //Record timestamp to csv
        TimeSpan time = timeline.Elapsed;
        questionTimestamp((int)time.TotalMinutes, (int)time.TotalSeconds);
        AudioSource.PlayClipAtPoint(response, new Vector3(16, 1, 11), 0.5f);
    }

    public void AddAlert(string alertType)
    {
        if (alertType == "Grab")
        {
            objectsGrabbed++;
        }
        TimeSpan timestamp = timeline.Elapsed;
        double stampTemp = (double)timestamp.Minutes + ((double)timestamp.Seconds / 60);
        stampTemp = Math.Round(stampTemp, 2);
        alerts.WriteLine(alertType + "," + stampTemp);
        alerts.Flush();
    }

    //Normally will put everything that is in OnApplicationQuit here
    void endSimulation()
    {
        print("FINISHED");
    }

    /* Runs when application closes
     * As stated above this should be moved into a seperate "endSimulation" function
     * The reason it hasn't already is the line: var filedone = SavWav.Save("userAudio", microphoneInput); uses a 3rd party library to save the user's audio
     * This library doesn't process fast enough to make the audio availble for playing in the next scene.
     */
    void OnApplicationQuit()
    {
        miscMetrics.WriteLine("Interrupts, " + interruptCounter);
        miscMetrics.WriteLine("Pauses, " + pauses);
        miscMetrics.WriteLine("Objects grabbed, " + objectsGrabbed);
        miscMetrics.Flush();

        //Closes the file
        questionResponses.Close();
        responseLengths.Close();
        alerts.Close();
        miscMetrics.Close();

        timeline.Stop();
        var filedone = SavWav.Save("userAudio", microphoneInput);
    }
}