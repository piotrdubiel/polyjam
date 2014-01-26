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

	PlayerAI getPlayerAI() {
		GameObject player = GameObject.FindGameObjectWithTag ("Player");
		PlayerAI ai = player.GetComponent ("PlayerAI") as PlayerAI;
		return ai;
	}

	void OnGUI() {
		PlayerAI ai = this.getPlayerAI();
		string score_content = "Mutation points: <b>" + ai.points + "</b>";

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
		int cost = MockStats.getCost (name);
		if (this.canUpgradeStatWithCost (cost)) {
			PlayerAI ai = this.getPlayerAI();
			ai.points -= cost;
			++ai.numberOfUpgrades;
			SpawnObjects spawner = this.getSpawnObjects();

			if (name.Equals ("fangs")) {
				ai.meatDesire += 1;
				spawner.plantSpawnFactor -= 0.04f;
				MeatBehaviour.Points += 100;
				PlantBehaviour.Points -= 20;
				MeatBehaviour.Food += 10;
				MockStats.fangs++;
			} else if (name.Equals("incisors")) {
				ai.plantDesire += 1;
				spawner.meatSpawnFactor -= 0.04f;
				PlantBehaviour.Points += 100;
				MeatBehaviour.Points -= 20;
				PlantBehaviour.Food += 6;
				MockStats.incisors++;
			} else if (name.Equals("hands")) {
				ai.maxHealth += 80;
				ai.health += 80;
				ai.strength += 2;
				ai.speed -= 0.2f;
				MockStats.hands++;
			} else if (name.Equals("legs")) {
				ai.maxHealth += 80;
				ai.health += 80;
				ai.strength -= 0.2f;
				ai.speed += 1.0f;
				MockStats.legs++;
			} else if (name.Equals("brain")) {
				ai.maxHealth += 120;
				ai.health += 120;
				spawner.meatSpawnFactor += 0.1f;
				spawner.plantSpawnFactor += 0.1f;
				MockStats.brain++;
			} else if (name.Equals("eyes")) {
				ai.maxHealth += 40;
				ai.health += 40;
				ai.meatDistance += 3;
				ai.plantDesire -= 0.2f;
				MockStats.eyes++;
			} else if (name.Equals("nose")) {
				ai.maxHealth += 40;
				ai.health += 40;
				ai.plantDistance += 3;
				ai.meatDesire -= 0.2f;
				MockStats.nose++;
			} else if (name.Equals("liver")) {
				MockStats.liver++;
			}

		}
	}

	SpawnObjects getSpawnObjects() {
		GameObject player = GameObject.FindGameObjectWithTag ("Tile Map");
		SpawnObjects spawner = player.GetComponent ("SpawnObjects") as SpawnObjects;
		return spawner;
	}

	bool canUpgradeStatWithCost (int cost) {
		PlayerAI ai = this.getPlayerAI ();
		if (ai.points >= cost) {
			return true;
		}
		return false;
	}
}
