/* ===========================================================
 *  PATileTerrainChunk.cs
 *  Copyright (C) 2011-2012, Pozdnyakov Anton. 
 * v1.04
 * =========================================================== */
using UnityEngine;
using System.Collections;

public class PATileTerrainChunk: MonoBehaviour 
{
	[System.Serializable]
    public class Settings
    {
		public int x, y, id;
		public Mesh mesh;				
	}
	public Settings settings = new Settings();

}
