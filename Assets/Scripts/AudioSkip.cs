using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class AudioSkip : MonoBehaviour {
	private float[] timestamps = new float[10];
	private String[] values;
    public List<float> list = new List<float>();
    // Use this for initialization
    void Start () {
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
        print("Question: " + question + " , " + list[0]);

        GameObject audio = GameObject.Find("MusicManager");
        audio.GetComponent<HajiyevMusicManager>().seek(list[0]);
    }

}
