using UnityEngine;
using System.Collections;

public class PanelBehaviour : MonoBehaviour {
	private Rect viewport;

	private int score = 0;
	public Texture active_texture;
	
	public GUIStyle header_style;
	public GUIStyle item_style;
	
	private int padding = 7;

	private int button_width = 25;
	private int row_height = 35;

	private int control_start = 60;

	void OnStart() {

	}

	void OnUpdate() {
	}

	void OnGUI() {

		string score_content = "Mutation points: <b>" +score + "</b>";

		GUILayout.BeginArea (new Rect (padding * 2, padding, camera.pixelWidth, control_start));
		GUILayout.Label (score_content, header_style);
		GUILayout.EndArea ();

		button_width = (int) (camera.pixelWidth - 4 * padding - 120) / 10 - 4;
		createStat (0, "fangs");
		createStat (1, "incisors");
		createStat (2, "hands");
		createStat (3, "legs");
		createStat (4, "brain");
		createStat (5, "eyes");
		createStat (6, "nose");
		createStat (7, "liver");

		GUI.DrawTexture(new Rect(padding, camera.pixelHeight - 80 - padding, 80, 80), active_texture);
	}

	private void createStat(int index, string name) {
		int stat = MockStats.getStat (name.Normalize());
		GUI.Label(new Rect(padding, control_start + index * row_height, 80, row_height), name, item_style);
		for (int i = 0; i < stat; ++i) {
			GUI.DrawTexture(new Rect(2 * padding + 80 + i * button_width, control_start + index * row_height, button_width, button_width), active_texture);
		}
		if (stat < 10) {
			if (GUI.Button (new Rect (2 * padding + 83 + stat * button_width, control_start + index * row_height, button_width, button_width), "+")) {
				buyStat (name);
			}
		}
		GUI.Label(new Rect(camera.pixelWidth - 60 - padding, control_start + index * row_height, 60, row_height), MockStats.getCost(name).ToString(), item_style);
	}

	private void buyStat(string name) {
		if (name.Equals ("fangs")) {
			MockStats.fangs++;
		}
	}

}
