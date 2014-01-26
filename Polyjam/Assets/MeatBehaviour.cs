using UnityEngine;

public class MeatBehaviour : FoodBehaviour
{
	public static float Points { get; set; }
	public static float Food { get; set; }
	public static float Speed { get; set; }
	public static float Strength { get; set; }
	public static float InitialHealth { get; set; }
	public static float SightDistance { get; set; }

	PATileTerrain terrain;
	Vector3 moveDirection;
	float timeToChangeMoveDirection;
	
	float changeInterval = 5.0f;
	// Use this for initialization
	void Start () {
		health = MeatBehaviour.InitialHealth;
		timeToChangeMoveDirection = changeInterval;
		
		GameObject go = GameObject.Find("Tile Map");
		if (go != null) terrain = go.GetComponent<PATileTerrain>();
		this.renderer.material.color = Color.red;
	}
	
	// Update is called once per frame
	void Update () {
		transform.localPosition += moveDirection * Time.deltaTime * MeatBehaviour.Speed;
		timeToChangeMoveDirection -= Time.deltaTime;
		if (terrain == null) {
			GameObject go = GameObject.Find("Tile Map");
			if (go != null) terrain = go.GetComponent<PATileTerrain>();
		}

		PlayerAI ai = getPlayerAI ();
		Vector3 direction = ai.transform.localPosition - transform.localPosition;

		if (direction.sqrMagnitude <= MeatBehaviour.SightDistance * MeatBehaviour.SightDistance) {
			timeToChangeMoveDirection = changeInterval;
			moveDirection = direction.normalized;
			if (direction.sqrMagnitude <= 1) {
				ai.SendMessageUpwards("Attack", this.gameObject);
			}
		} else if (timeToChangeMoveDirection <= 0) {
			changeMoveDirection();
		} else if (transform.localPosition.x <= 0 || transform.localPosition.x >= terrain.width ||
		           transform.localPosition.z <= 0 || transform.localPosition.z >= terrain.height) {
			print ("out of bounds");
			moveDirection *= -1;
			timeToChangeMoveDirection = changeInterval;
		}
	}
	
	void changeMoveDirection() {
		timeToChangeMoveDirection = changeInterval;
		moveDirection = this.randomMoveDirection ();
	}

	PlayerAI getPlayerAI() {
		GameObject go = GameObject.FindGameObjectWithTag ("Player");
		return go.GetComponent ("PlayerAI") as PlayerAI;
	}
	
	Vector3 randomMoveDirection() {
		float velocityX = Random.Range(-1000, 1000);
		float velocityZ = Random.Range(-1000, 1000);
		return new Vector3(velocityX, 0, velocityZ).normalized;
	}
}