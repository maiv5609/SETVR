using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class AudioSkip : MonoBehaviour {
	private float[] timestamps = new float[10];
	private String[] values;
    private GameObject audio;
    public List<float> list = new List<float>();
    // Use this for initialization
    void Start () {
        audio = GameObject.Find("MusicManager");
        using (var reader = new StreamReader(Application.dataPath + "/Resources/Visualizations/responseTimestamps.csv"))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                
                if(values[1] != "Timestamp (Minutes)")
                {
                    print(values[1]);
                    list.Add(float.Parse(values[1]));
                }
            }
        }
    }

    public void seek(int question)
    {
        print("Question: " + question + " , " + list[question-1]);
        var manager = audio.GetComponent<HajiyevMusicManager>();
        manager.seek(list[question-1]);
    }

}
