using UnityEngine;
using System.Collections;
using System;

public class GameOverGUI : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown (KeyCode.Space)) {
			print ("restart");
			Application.LoadLevel("sample");
		}
	}
}
