using UnityEngine;
using System.Collections;

public class PanelBehaviour : MonoBehaviour {
	public GUITexture panel;

	// Use this for initialization
	void Start () {
		transform.position = Vector3.zero;
		transform.localScale = Vector3.zero;
		panel.pixelInset = new Rect(0, 0, Screen.width / 2, Screen.height);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void onGUI() {
		GUI.Box (new Rect (0,0,100,50), "Top-left");
		GUI.Box (new Rect (Screen.width - 100,0,100,50), "Top-right");
		GUI.Box (new Rect (0,Screen.height - 50,100,50), "Bottom-left");
		GUI.Box (new Rect (Screen.width - 100,Screen.height - 50,100,50), "Bottom-right");

		if (GUI.Button (new Rect (10,10,150,100), "I am a button")) {
			print ("You clicked the button!");
		}
	}
}
