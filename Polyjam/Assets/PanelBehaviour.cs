using UnityEngine;
using System.Collections;

public class PanelBehaviour : MonoBehaviour {
	private Rect viewport;

	private int score = 0;
	public Texture activeTexture;
	public Texture[] healthTextures;
	
	public GUIStyle header_style;
	public GUIStyle item_style;
	
	private int padding = 7;

	private int button_width = 25;
	private int row_height = 35;

	private int control_start = 60;
	private int health;

	public SpriteRenderer legsSprite;
	public SpriteRenderer handSprite;
	public SpriteRenderer brainSprite;
	public SpriteRenderer kielSprite;
	public SpriteRenderer siekaczSprite;
	public SpriteRenderer watrobaSprite;
	public SpriteRenderer noseSprite;
	public SpriteRenderer eyesSprite;

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

		button_width = (int) (camera.pixelWidth - 83 - 4 * padding - 60) / 10;
		if (button_width > 25) {
			button_width = 25;
		}

		GUILayout.BeginArea (new Rect (padding * 2, padding, camera.pixelWidth, control_start));
		GUILayout.Label (score_content, header_style);
		GUILayout.EndArea ();

		createStat (0, "fangs");
		createStat (1, "incisors");
		createStat (2, "hands");
		createStat (3, "legs");
		createStat (4, "brain");
		createStat (5, "eyes");
		createStat (6, "nose");
		createStat (7, "liver");
	
		GUI.Label (new Rect(padding, camera.pixelHeight - 150 - padding, 80, 40), "Meat: " + ai.meatDesire);
		GUI.Label (new Rect(padding, camera.pixelHeight - 150 - padding - padding - 40, 80, 40), "Plant: " + ai.plantDesire);
		GUI.Label (new Rect(padding, camera.pixelHeight - 150 - padding - (padding + 40)*2, 80, 40), "Alc: " + ai.alcoholDesire);
		GUI.Label (new Rect(padding, camera.pixelHeight - 120 - padding, 80, 40), "Helath: " + ai.health + "/" + ai.maxHealth);
		GUI.Label (new Rect(padding, camera.pixelHeight - 90 - padding, 80, 40), "Poison: " + ai.poisoning);

		GUI.DrawTexture(new Rect(camera.pixelWidth - 80 - padding, camera.pixelHeight - 80 - padding, 80, 80), updateHealthTexture());
	}

	private void createStat(int index, string name) {
		int stat = MockStats.getStat (name);
		GUI.Label(new Rect(padding, control_start + index * row_height, 80, row_height), name.Normalize(), item_style);
		for (int i = 0; i < stat; ++i) {
			GUI.DrawTexture(new Rect(2 * padding + 80 + i * button_width, control_start + index * row_height, button_width, button_width), activeTexture);
		}
		if (stat < 10) {
			if (GUI.Button (new Rect (2 * padding + 83 + stat * button_width, control_start + index * row_height, button_width, button_width), "+")) {
				buyStat (name);
			}
		}
		if (MockStats.getStat(name) < 10) {
			GUI.Label(new Rect(camera.pixelWidth - 60 - padding, control_start + index * row_height, 60, row_height), MockStats.getCost(name).ToString(), item_style);
		}
	}

	private void buyStat(string name) {
		int cost = MockStats.getCost (name);
		if (this.canUpgradeStatWithCost (cost)) {
			PlayerAI ai = this.getPlayerAI();
			ai.points -= cost;
			++ai.numberOfUpgrades;
			SpawnObjects spawner = this.getSpawnObjects();
			MeatBehaviour.SightDistance *= 1.08f;
			MeatBehaviour.Speed *= 1.1f;
			MeatBehaviour.Strength *= 1.3f;
			if (name.Equals ("fangs")) {
				ai.meatDesire += 1;
				spawner.plantSpawnFactor -= 0.008f;
				MeatBehaviour.Points += 30;
				PlantBehaviour.Points -= 4;
				MeatBehaviour.Food += 20;
				MockStats.fangs++;
				kielSprite.sprite = Resources.Load<Sprite>("Player/kiel" + MockStats.fangs);
			} else if (name.Equals("incisors")) {
				ai.plantDesire += 1;
				spawner.meatSpawnFactor -= 0.008f;
				PlantBehaviour.Points += 20;
				MeatBehaviour.Points -= 4;
				PlantBehaviour.Food += 14;
				MockStats.incisors++;
				siekaczSprite.sprite = Resources.Load<Sprite>("Player/siekacz" + MockStats.incisors);
			} else if (name.Equals("hands")) {
				ai.maxHealth += 80;
				ai.health += 80;
				ai.strength += 2;
				ai.speed -= 0.3f;
				MockStats.hands++;
				handSprite.sprite = Resources.Load<Sprite>("Player/hand" + MockStats.hands);
			} else if (name.Equals("legs")) {
				ai.maxHealth += 80;
				ai.health += 80;
				ai.strength -= 0.2f;
				ai.speed += 1.0f;
				MockStats.legs++;
				legsSprite.sprite = Resources.Load<Sprite>("Player/legs" + MockStats.legs);
			} else if (name.Equals("brain")) {
				ai.maxHealth += 120;
				ai.health += 120;
				spawner.meatSpawnFactor += 0.02f;
				spawner.plantSpawnFactor += 0.02f;
				MockStats.brain++;
				brainSprite.sprite = Resources.Load<Sprite>("Player/brain" + MockStats.brain);
			} else if (name.Equals("eyes")) {
				ai.maxHealth += 40;
				ai.health += 40;
				ai.meatDistance += 7;
				ai.plantDesire -= 0.2f;
				MockStats.eyes++;
				eyesSprite.sprite = Resources.Load<Sprite>("Player/eyes" + MockStats.eyes);
			} else if (name.Equals("nose")) {
				ai.maxHealth += 40;
				ai.health += 40;
				ai.plantDistance += 7;
				ai.meatDesire -= 0.2f;
				MockStats.nose++;
				noseSprite.sprite = Resources.Load<Sprite>("Player/nose" + MockStats.nose);
			} else if (name.Equals("liver")) {
				ai.maxHealth += 40;
				ai.health += 40;
				ai.plantDistance -= 3.5f;
				ai.meatDistance -= 3.5f;
				ai.alcoholTolerance += 30;
				ai.alcoholDesire += 2.0f;
				MockStats.liver++;
				watrobaSprite.sprite = Resources.Load<Sprite>("Player/watroba" + MockStats.liver);
			}
			
			updateHealth(ai.health);
			print ("Stat: " + MockStats.getStat(name));
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

	void updateHealth(float h) {
		this.health = (int)(h * (healthTextures.Length - 1));
	}

	Texture updateHealthTexture() {
		if (health >= 0 && health < healthTextures.Length) {
			return healthTextures [health];
		}
		return healthTextures[healthTextures.Length-1];
	}
}
