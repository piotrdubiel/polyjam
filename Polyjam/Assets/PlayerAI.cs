// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------
using UnityEngine;

public class PlayerAI : MonoBehaviour
{
	PATileTerrain terrain;
	float _health;
	float _strength;
	float _speed;

	enum State {
		Thinking,
		Searching,
		Attacking
	}

	public PlayerAI() {
		speed = 0.5f;
		numberOfUpgrades = 0;
		PlantBehaviour.Points = 20;
		PlantBehaviour.Food = 5;
		MeatBehaviour.Points = 20;
		MeatBehaviour.Food = 10;
		points = 100;

		meatDesire = 0.0f;
		plantDesire = 0.0f;
		alcoholTolerance = 0.0f;
	}

	public float points { get; set; }
	public float health { get {
			return _health;
		}
		set
		{ 
			_health = Mathf.Min(value, maxHealth);
		}
	}
	public int numberOfUpgrades { get; set; }
	public float speed { get {
			return _speed + Mathf.Max(0, (poisoning - alcoholTolerance) * AlcoholBehaviour.SpeedFactor);
		}
		set {_speed = value;} 
	}
	public float strength { get {
			return _strength + Mathf.Max(0, (poisoning - alcoholTolerance) * AlcoholBehaviour.StrengthFactor);
		}
		set {_strength = value;} 
	}
	public float plantDistance { get; set; }
	public float meatDistance {get; set;}
	public float maxHealth { get; set; }
	public float plantDesire {get; set;}
	public float meatDesire { get; set; }
	public float alcoholDesire { get; set; }
	public float poisoningToHealth {get; set;}
	public float alcoholTolerance {get; set;}
	public float poisoningFall {get; set;}

	public Vector3 moveDirection;
	float timeToChangeMoveDirection;
	float timeBetweenAttacks = 1.0f;
	float attackInitialIntercal = 1.0f;
	float poisoning;

	float attackDistance = 1;
	
	float changeInterval = 5.0f;

	PanelBehaviour panel;

	State state;
	GameObject target;

	// Use this for initialization
	void Start () {
		moveDirection = this.randomMoveDirection ();

		GameObject go = GameObject.Find("Tile Map");
		if (go != null) terrain = go.GetComponent<PATileTerrain>();
		plantDistance = 2;
		meatDistance = 2;
		strength = 0;
		maxHealth = 100;
		health = 100;
		poisoningToHealth = 0.1f;
		alcoholTolerance = 0;
		poisoning = 0;
		poisoningFall = 1;
		panel = (PanelBehaviour) FindObjectOfType(typeof(PanelBehaviour));
		state = State.Searching;
	}
	
	// Update is called once per frame
	void Update () {
		this.updateHealth();
		this.movePlayer();
	}

	void updateHealth() {
		float poison = Mathf.Max (0.0f, poisoning - alcoholTolerance) * poisoningToHealth * 0.01f;
		print ("poison " + poison);
		health -= (0.05f + numberOfUpgrades * Time.deltaTime + poison);
		poisoning -= poisoningFall * Time.deltaTime;
		poisoning = Mathf.Max (poisoning, 0);
		panel.SendMessage ("updateHealth", health / maxHealth);
		if (health <= 0) {
			this.SendMessage ("playerDead");
			return;
		}
	}
	
	void movePlayer() {
		transform.localPosition += moveDirection * Time.deltaTime * this.speed;
		if (terrain == null) {
			GameObject go = GameObject.Find("Tile Map");
			if (go != null) terrain = go.GetComponent<PATileTerrain>();
		}

		if (state == State.Searching) {
			timeToChangeMoveDirection -= Time.deltaTime;
			if (timeToChangeMoveDirection <= 0) {
				changeMoveDirection ();
			} else {
				if (transform.localPosition.x <= 0 || transform.localPosition.x >= terrain.width) {
					timeToChangeMoveDirection = changeInterval;
					moveDirection.x *= -1;
				}
				if (transform.localPosition.z <= 0 || transform.localPosition.z >= terrain.height) {
					timeToChangeMoveDirection = changeInterval;
					moveDirection.z *= -1;
				}
			}
			this.search();
		} else {
			Vector3 dir = target.transform.localPosition - transform.localPosition;
			dir.y = 0;
			this.attackObject(target, dir.normalized, dir.sqrMagnitude);
		}
	}

	void search() {
		GameObject nearestPlant = this.nearestObjectWithTag("PlantBehaviour");
		Vector3 nearestPlantDirection = new Vector3();
		if (nearestPlant != null) {
			nearestPlantDirection = nearestPlant.transform.localPosition - transform.localPosition;
			nearestPlantDirection.y = 0;
			if (nearestPlantDirection.sqrMagnitude > plantDistance * plantDistance) {
				nearestPlant = null;
			}
		}
		
		GameObject nearestMeat = this.nearestObjectWithTag ("MeatBehaviour");
		Vector3 nearestMeatDirection = new Vector3();
		if (nearestMeat != null) {
			nearestMeatDirection = nearestMeat.transform.localPosition - transform.localPosition;
			nearestMeatDirection.y = 0;
			if (nearestMeatDirection.sqrMagnitude > meatDistance * meatDistance) {
				nearestMeat = null;
			}
		}
		
		GameObject nearestAlcohol = this.nearestObjectWithTag ("AlcoholBehaviour");
		Vector3 nearestAlcoholDirection = new Vector3();
		if (nearestAlcohol != null) {
			nearestAlcoholDirection = nearestAlcohol.transform.localPosition - transform.localPosition;
			nearestAlcoholDirection.y = 0;
			if (nearestAlcoholDirection.sqrMagnitude > 100) {
				nearestAlcohol = null;
			}
		}

		float alc = nearestAlcohol != null ? alcoholDesire : 0;
		float meat = nearestMeat != null ? meatDesire : 0;
		float plant = nearestPlant != null ? plantDesire : 0;
		float overallDesire = alc + meat + plant;
		if (overallDesire > 0) {
			float alcoholProb = alc / overallDesire;
			float meatProb = meat / overallDesire;
			float plantProb = plant / overallDesire;

			float prob = Random.Range(0, 1000) * 0.001f;
			if (prob < alcoholProb) {
				target = nearestAlcohol;
			} else if (prob < alcoholProb + meatProb) {
				target = nearestMeat;
			} else if (prob < overallDesire) {
				target = nearestPlant;
			}
			state = State.Attacking;
		}
	}

	GameObject nearestObjectWithTag(string name) {
		GameObject[] gos = GameObject.FindGameObjectsWithTag(name);
		GameObject nearestPlant = null;
		foreach (GameObject go in gos) {
			if (nearestPlant == null) {
				nearestPlant = go;
			} else {
				Vector3 oldDistance = nearestPlant.transform.localPosition - transform.localPosition;
				Vector3 newDistance = go.transform.localPosition - transform.localPosition;
				if (newDistance.sqrMagnitude < oldDistance.sqrMagnitude) {
					nearestPlant = go;
				}
			}
		}
		return nearestPlant;
	}

	void attackObject (GameObject go, Vector3 direction, float distance) {
		timeToChangeMoveDirection = changeInterval;
		moveDirection = direction;
		
		timeBetweenAttacks -= Time.deltaTime;

		if (distance < attackDistance && timeBetweenAttacks <= 0) {
			timeBetweenAttacks = attackInitialIntercal;
			go.SendMessageUpwards ("Attack", this.gameObject);
		}
	}
	
	void changeMoveDirection() {
		timeToChangeMoveDirection = changeInterval;
		moveDirection = this.randomMoveDirection ();
	}
	
	Vector3 randomMoveDirection() {
		float velocityX = Random.Range(-1000, 1000);
		float velocityZ = Random.Range(-1000, 1000);
		return new Vector3(velocityX, 0, velocityZ).normalized;
	}
	
	void playerDead() {
		Application.LoadLevel ("game over");
	}

	void killed(GameObject go) {
		if (go.tag.Equals ("PlantBehaviour")) {
			this.points += PlantBehaviour.Points;
			this.health += PlantBehaviour.Food;
		} else if (go.tag.Equals ("MeatBehaviour")) {
			this.points += MeatBehaviour.Points;
			this.health += MeatBehaviour.Food;
		} else if (go.tag.Equals ("AlcoholBehaviour")) {
			print ("Alcohol");
			poisoning += AlcoholBehaviour.PoisonAmount;

		}
		state = State.Searching;
		target = null;
	}

	void Attack(GameObject go) {
		this.health -= MeatBehaviour.Strength;
		this.state = State.Attacking;
		this.target = go;
		if (health <= 0) {
			this.playerDead();
		}
	}
}


