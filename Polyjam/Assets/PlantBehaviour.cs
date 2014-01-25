using UnityEngine;
using System.Collections;

public class PlantBehaviour : FoodBehaviour {
	// Use this for initialization
	void Start () {
		amountOfFood = 10;
		pointsForEating = 10;
		health = 3;
		this.renderer.material.color = Color.green;
	}
	
	// Update is called once per frame
	void Update () {

	}
}
