using UnityEngine;
using System.Collections;

public class automove : MonoBehaviour {

	enum MoveDirection {
		Up,
		Down,
		Right,
		Left
	}

	MoveDirection direction;
	float speed;
	// Use this for initialization
	void Start () {
		direction = MoveDirection.Right;
		speed = 0.3f;
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 position = transform.position;
		if (direction == MoveDirection.Right && position.x >= 94.0f) {
				direction = MoveDirection.Up;
		} else if (direction == MoveDirection.Up && position.z >= 64.0f) {
				direction = MoveDirection.Left;
		} else if (direction == MoveDirection.Left && position.x <= 30.0f) {
				direction = MoveDirection.Down;
		} else if (direction == MoveDirection.Down && position.z <= 0) {
				direction = MoveDirection.Right;
		}

		switch (direction) {
			case MoveDirection.Down:
				position.z -= speed;
				break;
			case MoveDirection.Left:
				position.x -= speed;
				break;
			case MoveDirection.Right:
				position.x += speed;
				break;
			case MoveDirection.Up:
				position.z += speed;
				break;
		}
		transform.position = position;
	}
}
