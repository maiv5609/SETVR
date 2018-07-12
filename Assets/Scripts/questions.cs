using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class questions : MonoBehaviour {
	//Current indexes for lists
	int index1 = 0;
	int index2 = 0;
	int index3 = 0;
	//Lists to store questions
	List<string> phase1 = new List<string>();
	List<string> phase2 = new List<string>();
	List<string> phase3 = new List<string>();

	//Audioclip for interviewer responses
	private Microphone mic;
	public int voiceSampleSize;
	public float isSpeakingThreshold;
	AudioClip response;

	// Populates Questions
	void Start () {
		
		int counter = 0;
		string line;
		string testComponent;
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

		Invoke ("nextQuestion", 4);
		 
	}

	public void nextQuestion(){
		int currIndex = 0;

		System.Random genRand = new System.Random();
		//phase 1 questions
		if (index1 == 0){
			//TODO: Test detecting when audio is playing and if user is talking during audio
			GameObject.Find("QuestionText").GetComponent<Text>().text = " Current Question: " + phase1[0];
			//Load audio and play at point
			//interviewerSpeak(1, 1);

			phase1.RemoveAt(0);
			index1++;
		}else if(index2 == 0 && index3 == 0){
			//TODO: remove $$ index2 == 0 when we want more questions from random pool
			//Check that we aren't in phase 3 
			//Select random index from phase2
			currIndex = genRand.Next(0, phase2.Count-1);
			GameObject.Find("QuestionText").GetComponent<Text>().text = " Current Question: " + phase2[currIndex];
			phase2.RemoveAt(currIndex);
			index2++;
		}else if(index3 == 0){
			//Phase 3
			GameObject.Find("QuestionText").GetComponent<Text>().text = " Current Question: " + phase3[0];
			phase3.RemoveAt(0);
			index3++;
		}else{
			GameObject.Find("QuestionText").GetComponent<Text>().text = "End of Questions";
		}
	}

	/* Takes in current interview phase and selected question
	 * Plays selected audio clip for question, detects if user is speaking while interviewer is speaking
	 */
	float interviewerSpeak(int phase, int question){
		string[] devices = Microphone.devices;
		AudioClip sampleClip; //Test clip
		AudioSource audio = GetComponent<AudioSource> ();
		float sum = 0;
		float voiceVolume = 0;
		float[] voiceData = new float[voiceSampleSize];

		//TODO: Test out pausing audio clip in response to the user speaking, track number of pauses.
		//Explore alternative responses to the user talking while the interviewer is speaking.
		response = Resources.Load<AudioClip>("QuestionAudio/P1Q1");
		//audio.PlayOneShot ((AudioClip)Resources.Load ("QuestionAudio/P1Q1"));
		AudioSource.PlayClipAtPoint (response, new Vector3(16, 1, 11));

		//Testing recording audio clips
		sampleClip = Microphone.Start(devices [0], true, 10, 44100);
		SavWav.Save ("questionsVoiceTestFile", sampleClip);

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
