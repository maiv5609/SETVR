using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfficeSound : MonoBehaviour {

	// Use this for initialization
	void Start () {

		AudioSource sound = GetComponent<AudioSource> ();
		sound.Play ();
		sound.loop = true;

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
