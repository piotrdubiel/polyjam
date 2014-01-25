using UnityEngine;
using System.Collections;

public class PanelBehaviour : MonoBehaviour {
	public GUITexture panel;

	// Use this for initialization
	void Start () {
		panel = GetComponent<GUITexture> ();

		transform.position = Vector3.zero;
		transform.localScale = Vector3.zero;
		panel.pixelInset = new Rect(0, 0, Screen.width / 2, Screen.height);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void onGUI() {
		if (GUI.Button (new Rect (10,10,150,100), "I am a button")) {
			print ("You clicked the button!");
		}
	}
}
