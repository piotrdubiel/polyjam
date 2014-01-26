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
			MockStats.fangs = 0;
			MeatBehaviour.Food = 10;
			MeatBehaviour.Points = 100;
			PlantBehaviour.Food = 5;
			PlantBehaviour.Points = 100;
			Application.LoadLevel("sample");
		}
	}
}
