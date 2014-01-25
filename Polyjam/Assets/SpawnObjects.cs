using UnityEngine;
using System.Collections;

public class SpawnObjects : MonoBehaviour {

	float rate = 1.0f;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (Time.time % rate < 0.02) {
			print("Spawn");
			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.transform.parent = transform;
			go.transform.localPosition = new Vector3(Random.Range(0, transform.localScale.x),
			                                    0, Random.Range(0, transform.localScale.z));
			PlayerAI pai = go.AddComponent("PlayerAI") as PlayerAI;
			pai.speed = Random.Range(1, 20) * 0.1f;
		}
	}
}
