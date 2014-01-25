using UnityEngine;
using System.Collections;

public class PanelBehaviour : MonoBehaviour {
	private Rect viewport;

	private int score = 0;
	public GUISkin skin;

	private int button_width;
	void OnStart() {
	}

	void OnUpdate() {
	}

	void OnGUI() {
		GUI.skin = skin;
		int padding = 10;
		GUI.color = Color.black;

		string score_content = "Mutation points: ";


		GUILayout.BeginVertical ();
		GUILayout.BeginHorizontal ();
		GUILayout.Label (score_content);
		GUILayout.Label (score.ToString());
		GUILayout.EndHorizontal ();

		GUILayout.BeginArea (new Rect (padding, 2 * padding + 20, 90, camera.pixelHeight));
		GUILayout.Label ("Kły");
		GUILayout.Label ("Siekacze");
		GUILayout.Label ("Nogi");
		GUILayout.Label ("Ręce");
		GUILayout.Label ("Oczy");
		GUILayout.Label ("Wątroba");
		GUILayout.Label ("Wątroba");
		GUILayout.Label ("Wątroba");
		GUILayout.EndArea ();

		GUILayout.BeginArea (new Rect (padding * 2 + 50, 2 * padding + 20, 40, camera.pixelHeight));
		GUILayout.Label (MockStats.getCost("fangs").ToString());
		GUILayout.Label (MockStats.getCost("incisors").ToString());
		GUILayout.Label (MockStats.getCost("legs").ToString());
		GUILayout.Label (MockStats.getCost("hands").ToString());
		GUILayout.Label (MockStats.getCost("eyes").ToString());
		GUILayout.Label (MockStats.getCost("liver").ToString());
		GUILayout.Label (MockStats.getCost("fangs").ToString());
		GUILayout.Label (MockStats.getCost("fangs").ToString());
		GUILayout.EndArea ();

		GUILayout.BeginArea (new Rect (padding * 3 + 120, 2 * padding + 20, camera.pixelWidth - 4 * padding - 120, camera.pixelHeight));
		button_width = (int) (camera.pixelWidth - 4 * padding - 120) / 10 - 4;
		createStat ("fangs");
		createStat ("incisors");
		createStat ("legs");
		createStat ("hands");
		createStat ("eyes");
		createStat ("liver");
		createStat ("liver");
		createStat ("liver");
		
		GUILayout.EndArea ();
		GUILayout.EndVertical ();



	}

	private void createStat(string name) {
		GUILayout.BeginHorizontal ();
		for (int i = 0; i < MockStats.getStat(name); ++i) {
			GUILayout.Button ("#", GUILayout.Width(button_width));	
		}
		
		if (GUILayout.Button ("+",  GUILayout.Width (button_width))) {
			buyStat(name);
		}
		for (int i = MockStats.getStat(name) + 1; i < 10; ++i) {
			GUILayout.Button ("  ", GUILayout.Width(button_width));	
		}
		GUILayout.EndHorizontal ();
	}

	private void buyStat(string name) {
		print (name);
	}

}
