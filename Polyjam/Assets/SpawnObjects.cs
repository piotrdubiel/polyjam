using UnityEngine;
using System.Collections;

public class SpawnObjects : MonoBehaviour {

	PATileTerrain terrain;

	float initialTimeToSpawn = 1.0f;
	float _plantSpawnFactor;
	float _meatSpawnFactor;
	float _alcoholSpawnFactor;

	public float plantSpawnFactor { get {return _plantSpawnFactor;} set{_plantSpawnFactor = Mathf.Max(0, _plantSpawnFactor);} }
	float timeToSpawnPlant;

	public float meatSpawnFactor { get {return _meatSpawnFactor;} set{_meatSpawnFactor = Mathf.Max(0, _meatSpawnFactor);} }
	float timeToSpawnMeat;

	public float alcoholSpawnFactor { get {return _alcoholSpawnFactor;} set{_alcoholSpawnFactor = Mathf.Max(0, _alcoholSpawnFactor);} }
	float timeToSpawnAlcohol;
	// Use this for initialization
	void Start () {
		MeatBehaviour.InitialHealth = 5;
		MeatBehaviour.Speed = 2;
		MeatBehaviour.Strength = 1;
		MeatBehaviour.SightDistance = 10;
		timeToSpawnMeat = initialTimeToSpawn;
		timeToSpawnPlant = initialTimeToSpawn;
		_alcoholSpawnFactor = initialTimeToSpawn;
		GameObject go = GameObject.Find("Tile Map");
		if (go != null) terrain = go.GetComponent<PATileTerrain>();
		plantSpawnFactor = 0.2f;
		meatSpawnFactor = 0.2f;
		alcoholSpawnFactor = 0.08f;
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
