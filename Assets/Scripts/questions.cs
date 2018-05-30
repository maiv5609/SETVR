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
	
	// Populates Questions
	void Start () {
		
		int counter = 0;
		string line;
		string testComponent;
		string path;
		path = Application.dataPath;
		
		//Read in phase 1 questions
		System.IO.StreamReader file = new System.IO.StreamReader(path + "/Questions/Phase1.txt");
		while((line = file.ReadLine()) != null){
			phase1.Add(line);
		}
		
		//Read in phase 2 questions
		file = new System.IO.StreamReader(path + "/Questions/Phase2.txt");
		while((line = file.ReadLine()) != null){
			phase2.Add(line);
		}
		
		//TODO: remove this test print
		/*
		UnityEngine.Debug.Log("Length:" + phase2.Count);
		while(counter != phase2.Count){
			UnityEngine.Debug.Log(phase2[counter]);
			counter++;
		}
		counter = 0;
		*/
		
		//Read in phase 3 questions
		file = new System.IO.StreamReader(path + "/Questions/Phase3.txt");
		while((line = file.ReadLine()) != null){
			phase3.Add(line);
		}	
		
		testComponent = GameObject.Find("QuestionText").GetComponent<Text>().text;
		UnityEngine.Debug.Log("Found: " + testComponent);
		
		
	}

	public void nextQuestion(){
		int currIndex = 0;
		System.Random genRand = new System.Random();
		//phase 1 questions
		if (index1 == 0){
			GameObject.Find("QuestionText").GetComponent<Text>().text = " Current Question: " + phase1[0];
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
}
