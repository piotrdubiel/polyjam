using UnityEngine;
using System.Collections;

public class SpawnObjects : MonoBehaviour {

	PATileTerrain terrain;

	float initialTimeToSpawn = 1.0f;

	float plantSpawnFactor = 0.2f;
	float timeToSpawnPlant;

	float meatSpawnFactor = 0.2f;
	float timeToSpawnMeat;
	// Use this for initialization
	void Start () {
		timeToSpawnMeat = initialTimeToSpawn;
		timeToSpawnPlant = initialTimeToSpawn;
		GameObject go = GameObject.Find("Tile Map");
		if (go != null) terrain = go.GetComponent<PATileTerrain>();
	}
	
	// Update is called once per frame
	void Update () {
		this.tryToSpawnPlant();
		this.tryToSpawnMeat ();
	}

	void tryToSpawnMeat() {
		float random = Random.Range (0, 1000) * 0.001f;
		timeToSpawnMeat -= Time.deltaTime;
		if (timeToSpawnMeat <= 0) {
			timeToSpawnMeat = initialTimeToSpawn;
			if (random <= meatSpawnFactor) {
				this.spawnObjectWithBehaviour("MeatBehaviour");
			}
		}
	}
	
	void tryToSpawnPlant() {
		float random = Random.Range (0, 1000) * 0.001f;
		timeToSpawnPlant -= Time.deltaTime;
		if (timeToSpawnPlant <= 0) {
			timeToSpawnPlant = initialTimeToSpawn;
			if (random <= plantSpawnFactor) {
				this.spawnObjectWithBehaviour("PlantBehaviour");
			}
		}
	}

	void spawnObjectWithBehaviour(string behaviour) {
		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.transform.parent = transform;
		go.transform.localPosition = new Vector3(Random.Range(0, terrain.width),
		                                         0, Random.Range(0, terrain.height));
		go.AddComponent (behaviour);
		go.tag = behaviour;
		print ("Spawn " + behaviour);
	}
}
