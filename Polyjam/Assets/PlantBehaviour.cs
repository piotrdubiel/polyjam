using UnityEngine;
using System.Collections;

public class PlantBehaviour : FoodBehaviour {
	public static float Points { get; set; }
	public static float Food { get; set; }

	// Use this for initialization
	void Start () {
		health = 3;
		(this.GetComponent("SpriteRenderer") as SpriteRenderer).sprite = Resources.Load<Sprite> ("plant");
	}
	
	// Update is called once per frame
	void Update () {

	}
}
