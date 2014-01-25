using UnityEngine;

public class MeatBehaviour : FoodBehaviour
{
	PATileTerrain terrain;
	Vector3 moveDirection;
	float timeToChangeMoveDirection;
	float speed;
	
	float changeInterval = 5.0f;
	// Use this for initialization
	void Start () {
		amountOfFood = 10;
		pointsForEating = 10;
		health = 5;
		speed = 0.3f;
		timeToChangeMoveDirection = changeInterval;
		
		GameObject go = GameObject.Find("Tile Map");
		if (go != null) terrain = go.GetComponent<PATileTerrain>();
		this.renderer.material.color = Color.red;
	}
	
	// Update is called once per frame
	void Update () {
		transform.localPosition += moveDirection * Time.deltaTime;
		timeToChangeMoveDirection -= Time.deltaTime;
		if (terrain == null) {
			GameObject go = GameObject.Find("Tile Map");
			if (go != null) terrain = go.GetComponent<PATileTerrain>();
		}

		if (terrain == null) {
			print ("no terrain");
		}

		print ("position " + transform.localPosition + "terrain " + terrain.width + " " + terrain.height);
		if (timeToChangeMoveDirection <= 0) {
			changeMoveDirection();
		} else if (transform.localPosition.x <= 0 || transform.localPosition.x >= terrain.width ||
		           transform.localPosition.z <= 0 || transform.localPosition.z >= terrain.height) {
			print ("out of bounds");
			moveDirection *= -1;
		}
	}
	
	void changeMoveDirection() {
		timeToChangeMoveDirection = changeInterval;
		moveDirection = this.randomMoveDirection ();
	}
	
	Vector3 randomMoveDirection() {
		float velocityX = Random.Range(0, 1000) * 0.001f;
		float velocityZ = Random.Range(0, 1000) * 0.001f;
		return new Vector3(velocityX, 0, velocityZ).normalized;
	}
}