using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InGameEditing : MonoBehaviour 
{
	public PATileTerrain terrain = null;
	
	private bool pressed = false;
	private List<PATileTerrain.PAPointXY> changedPoints = new List<PATileTerrain.PAPointXY>();
	
	void Awake()
	{
		if (terrain == null) 
		{
			GameObject go = GameObject.Find("PATileTerrain");
			if (go != null) terrain = go.GetComponent<PATileTerrain>();
		}
	}
	
	void Start() 
	{
		
	}
	
	void Update () 
	{
		RaycastHit hit;
		Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
		
		if (!pressed && Input.GetMouseButtonDown(0)) pressed = true;
		if (pressed && Input.GetMouseButtonUp(0))
		{
			pressed = false;
			terrain.UpdateMesh(changedPoints);
			changedPoints.Clear();
		}
		
		if (pressed && Physics.Raycast(ray, out hit, Mathf.Infinity)) 
		{
			if (terrain.IsTerrain(hit.transform) != null) 
			{
				Vector3 pos;
				int x, y;
				float p = 0.1f;
				
				/*[1.04]*/ //pos = hit.point - terrain.transform.position;
				pos = terrain.transform.InverseTransformPoint(hit.point);
				pos += new Vector3(terrain.tileSize / 2, 0.0f, terrain.tileSize / 2);
				x = (int)Mathf.Abs(pos.x / terrain.tileSize);
				y = (int)Mathf.Abs(pos.z / terrain.tileSize);
							
				terrain.DeformPointTerrain(x, y, false, p, 4.0f, changedPoints);
					
			}
		}
	}
}
