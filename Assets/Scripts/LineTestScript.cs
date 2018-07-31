using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LineTestScript : MonoBehaviour {

    // Name of the input file, no extension
    public string inputfile;
    
    // Initialize our output
    public static string output;

    // List for holding data from CSV parser
    private List<Dictionary<string, object>> pointList;

    // The prefab for the data will be shown in
    public GameObject CanvasImage;

    //Reference to the Text component
    Text text;

	// Use this for initialization
	void Start () {
        // Set up text reference
        text = GetComponent<Text>();

        //Set data points to fill in list
        pointList = CSVParser.Read(inputfile);

        for (var i = 0; i < pointList.Count; i++)
        {

        }

        //Set the text to what we want
        text.text = "Average: " + output;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
