using UnityEngine;
using System.Collections;

public class PanelBehaviour : MonoBehaviour {

	private Rect viewport;

	void OnStart() {
	}
	void OnUpdate() {
		}
	void OnGUI() {
		int padding = 10;
		int button_width = (int) (camera.pixelWidth - 4 * padding) / 3;

		if (GUI.Button (new Rect (10, 10, button_width , 50), "Gene 1")) {
			print ("You clicked the button!");

		}

		if (GUI.Button (new Rect (10 + button_width + 10, 10, button_width , 50), "Gene 2")) {
			print ("You clicked the button!");

		}

		if (GUI.Button (new Rect (30 + 2 * button_width, 10, button_width , 50), "Gene 3")) {
			print ("You clicked the button!");

		}
	}

}
