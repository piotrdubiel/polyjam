using UnityEngine;

public class MeatBehaviour : FoodBehaviour
{
	public static float Points { get; set; }
	public static float Food { get; set; }

	PATileTerrain terrain;
	Vector3 moveDirection;
	float timeToChangeMoveDirection;
	float speed;
	
	float changeInterval = 5.0f;
	// Use this for initialization
	void Start () {
		health = 5;
		speed = 0.3f;
		timeToChangeMoveDirection = changeInterval;
		
		GameObject go = GameObject.Find("Tile Map");
		if (go != null) terrain = go.GetComponent<PATileTerrain>();
		this.renderer.material.color = Color.red;
	}
	
	// Update is called once per frame
	void Update () {
		transform.localPosition += moveDirection * Time.deltaTime * this.speed;
		timeToChangeMoveDirection -= Time.deltaTime;
		if (terrain == null) {
			GameObject go = GameObject.Find("Tile Map");
			if (go != null) terrain = go.GetComponent<PATileTerrain>();
		}

		if (timeToChangeMoveDirection <= 0) {
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
	
	Vector3 randomMoveDirection() {
		float velocityX = Random.Range(-1000, 1000);
		float velocityZ = Random.Range(-1000, 1000);
		return new Vector3(velocityX, 0, velocityZ).normalized;
	}
}