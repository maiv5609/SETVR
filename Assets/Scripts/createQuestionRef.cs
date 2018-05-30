using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class createQuestionRef : MonoBehaviour {

	Text questionText;

	// Initialization for current question ref
	void Start () {
		questionText = GetComponent<Text>();
	}
	
}
