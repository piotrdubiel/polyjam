using UnityEngine;
using System.Collections;

public class PanelBehaviour : MonoBehaviour {
	private Rect viewport;

	private int score = 0;
	void OnStart() {
	}
	void OnUpdate() {
		}
	void OnGUI() {
		int padding = 10;
		int button_width = (int) (camera.pixelWidth - 4 * padding) / 3;

		string score_content = "Mutation points: " + score;

		GUI.Box(new Rect(padding, padding, camera.pixelWidth - 2 * padding, 20), score_content);
		GUILayout.BeginArea (new Rect (padding, 2 * padding + 20, 100, camera.pixelHeight));

		GUILayout.Label ("Kły");
		GUILayout.Label ("Siekacze");
		GUILayout.Label ("Nogi");
		GUILayout.Label ("Ręce");
		GUILayout.Label ("Oczy");
		GUILayout.Label ("Wątroba");
		GUILayout.Label ("Wątroba");
		GUILayout.Label ("Wątroba");
		GUILayout.EndArea ();

		GUILayout.BeginArea (new Rect (padding * 2 + 100, 2 * padding + 20, camera.pixelWidth - 3 * padding - 100, camera.pixelHeight));

		createStat ("fangs");
		createStat ("incisors");
		createStat ("legs");
		createStat ("hands");
		createStat ("eyes");
		createStat ("liver");
		createStat ("liver");
		createStat ("liver");
		
		GUILayout.EndArea ();

	}

	private void createStat(string name) {
		GUILayout.BeginHorizontal ();
		for (int i = 0; i < MockStats.getStat(name); ++i) {
			GUILayout.Button ("#", GUILayout.Width(20));	
		}
		
		if (GUILayout.Button ("+", GUILayout.Width (20))) {
			print ("Add " + name);		
		}
		for (int i = MockStats.getStat(name) + 1; i < 10; ++i) {
			GUILayout.Button ("  ", GUILayout.Width(20));	
		}
		GUILayout.EndHorizontal ();
	}

}
