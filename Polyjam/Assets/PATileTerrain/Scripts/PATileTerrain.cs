/* ===========================================================
 *  PATileTerrain.cs
 *  Copyright (C) 2011-2012, Pozdnyakov Anton. 
 * v1.04
 * =========================================================== */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PATileTerrain: MonoBehaviour 
{
	[System.Serializable]
	public class EditorSettings
    {
        public int x = 32;
		public int y = 32;
		public float tileSize = 1.0f;
		public float maxHeight = 50.0f, minHeight = -50.0f;
		public Material tileSetMaterial;
		
		public int tilesetX = 1;
		public int tilesetY = 1;
		
		public int chunkSize = 8;
		public string name = "Terrain";
		
    }
	public EditorSettings editorSettings = new EditorSettings();
	
	[System.Serializable]
	public class PATile
	{	
		public int id = -1; //tile id
		public int chunkId = -1; //[read only] chunk id
		public int x = -1; //[read only] X 
		public int y = -1; //[read only] Y
		public int cx = -1; //[read only] X in the chunk
		public int cy = -1; //[read only] Y in the chunk
		public int cId = -1;
		public string name; //tile name
		
		public Vector3 position; //helper position
			
		//removed in v1.01
		//Data
		//public Vector3[] verts = new Vector3[4];
		//public int[] tris = new int[6];
		//public Vector2[] uvs = new Vector2[4];
		//public Color[] colors = new Color[4];
		//public Vector3[] normals = new Vector3[4];		
		
		public int type = -1;
		public int toType = -1;
		public byte bits = 0;
		
		//Game data
		public bool walkability = true; //for PathFinder
		public Object customData; //User data, you can assign your object
	}
	
	[System.Serializable]
	public class PAPointXY 
	{ 
		public int x;
		public int y; 
		public PAPointXY(int xx, int yy) { x = xx; y = yy; }
		public override bool Equals(object obj) { if (((PAPointXY)obj).x == this.x && ((PAPointXY)obj).y == this.y)  return true; return false; }
		public override int GetHashCode() { return this.GetHashCode(); }
		public static bool operator==(PAPointXY p1, PAPointXY p2) { return p1.Equals(p2); }
        public static bool operator!=(PAPointXY p1, PAPointXY p2) { return !(p1 == p2); }
		//public override string ToString() { return "("+x + ", " + y +")"; }
	}
	
	[System.Serializable]
	public class PAPoint
	{		
		public int[] t = new int[4]; //Tile index
		public int[] p = new int[4]; //Tile vertex
	}
	
	public struct PATileUV 
	{ 
		public Vector2 p0, p1, p2, p3; 
	}
	
	[System.Serializable]
	public class PATSType
	{
		public int id = -1;
		public string name = "";
		
		public List<int> baseIndexes = new List<int>();
		
		public PATSType() { AddBaseIndex(); }
		public void AddBaseIndex() { baseIndexes.Add(0); }
		public void AddBaseIndex(int i) { baseIndexes.Add(i); }
		public void RemoveBaseIndex(int i) { if (baseIndexes.Count <= 0) AddBaseIndex();	if (baseIndexes.Count <= 1) return; baseIndexes.RemoveAt(i); }
		
		//Editor helpers
		public bool show = true;
	}
	
	[System.Serializable]
	public class PATSTransition //example: dirt(transition) <-> grass(type)
	{
		public int from = -1;
		public int to = -1;
		public string name = "";
		
		public int[] transitions = new int[14];
		
		public PATSTransition() { for (int i = 0; i < 14; ++i) transitions[i] = 0; }
		
		//Editor helpers
		public bool show = true;
	}
	
	[System.Serializable]
	public class PathData
    {
		public PATile[] data = null; //path tiles
		public int length = 0; //path length
		public bool found = false; //founded?
	}
	
	[System.Serializable]
    public class Settings
    {
		public bool created = false;
		public bool finalized = false;
		
		public PATileTerrainChunk[] chunks = null; //chunks
		public PATile[] tiles = null; //array of all tiles
		public PAPoint[] points = null; //helper point for edit the terrain
		public int xCount, yCount; //number of tiles along the X and Y (X and Z in the Unity3d space)
		public int chunkCountX, chunkCountY; //number of chunks along the X and Y
		public int chunkSize; //size of a one chunk
		public float tileSize; //size of a tile
		public Vector2 uvOffset; //uv offset for edit and painting
		public float maxHeight, minHeight; //max and min allowed height
		public float diagonalLength; 
		
		//For editor
		//TileSet
		public Material tilesetMaterial;
		public int tilesetX;
		public int tilesetY;
		public int tilesetCount;
		public float tilesetWidth;
		public float tilesetHeight;
		
		public string name;
		
		public List<PATSTransition> tsTrans = new List<PATSTransition>();
		public List<PATSType> tsTypes = new List<PATSType>();
				
	}
	public Settings settings = new Settings();
	
	
	protected class IntermediateInfo
	{
		public int fromType, toType;
		public int imFromType, imToType;
	}
	private int[] TRANSITION_BITS = 
	{
		8,  //0  - 1000 
		4,  //1  - 0100 
		2,  //2  - 0010 
		1,  //3  - 0001 
		12, //4  - 1100 
		3,  //5  - 0011 
		13, //6  - 1101 
		14, //7  - 1110
		7,  //8  - 0111 
		11, //9  - 1011 
		5,  //10 - 0101 
		10, //11 - 1010
		9,  //12 - 1001 
		6   //13 - 0110
	}; 
	
	void Awake() { }
	void Start() { }
	void Update() {	}
	

	public PATile GetTile(int x, int y) { return settings.tiles[settings.xCount * y + x]; }
	public PATile GetTile(int index) { return settings.tiles[index]; }
	public PATileTerrainChunk GetChunk(int x, int y) { return settings.chunks[settings.chunkCountX * y + x]; }
	public PATileTerrainChunk GetChunk(int index) { return settings.chunks[index]; }
	
	public float GetHeight(Vector3 pos)
	{
		pos = pos - transform.position;
		
		if (pos.x >= 0 && pos.x < settings.xCount * settings.tileSize &&
		    pos.z >= 0 && pos.z < settings.yCount * settings.tileSize)
		{
			int x = (int)(pos.x / settings.tileSize);
        	int y = (int)(pos.z / settings.tileSize);
			
			float xt = pos.x - x * settings.tileSize;
        	float yt = pos.z - y * settings.tileSize;
			
			Vector3 intersection = Vector3.zero;
			Vector3 pointA, pointB, pointC;
			if (xt <= yt)
        	{
				pointA = new Vector3(0.0f, GetPointHeight(x, y), 0.0f);
            	pointB = new Vector3(0.0f, GetPointHeight(x, y + 1), settings.tileSize);
            	pointC = new Vector3(settings.tileSize, GetPointHeight(x + 1, y + 1), settings.tileSize);
			} else
			{
				pointA = new Vector3(0, GetPointHeight(x, y), 0);
            	pointB = new Vector3(settings.tileSize, GetPointHeight(x + 1, y + 1), settings.tileSize);
            	pointC = new Vector3(settings.tileSize, GetPointHeight(x + 1, y), 0.0f);
			}
			
			Vector3 linePoint = new Vector3(xt, 0.0f, yt);
			Vector3 lineNormal = (Vector3.Cross((pointB - pointA), (pointC - pointA))).normalized;
			float t2 = Vector3.Dot(lineNormal, Vector3.up); 
			
			if (t2 != 0)
			{
				float d = Vector3.Dot(pointA, lineNormal);
				float t = - (Vector3.Dot(lineNormal, linePoint) - d) / t2;
				
				intersection = linePoint + Vector3.up * t;
				
				if (IsOnSameSide(intersection, pointA, pointB, pointC) &&
					IsOnSameSide(intersection, pointB, pointA, pointC) &&
					IsOnSameSide(intersection, pointC, pointA, pointB))
				{
					return intersection.y + transform.position.y;
				}
			}
			
			return intersection.y + transform.position.y;
			
		}
		
		return 0.0f;
	}
		
	//Edit Methods 
	/*public PATile GetTileByTriangleIndex(int triangleIndex)
	{
		//Triangle index to tile index
		if ((triangleIndex & 0x01) == 1) triangleIndex--;
		triangleIndex /= 2;		
		return settings.tiles[triangleIndex];
	}*/
	
	public Mesh GetChunkMesh(int chunk) { return settings.chunks[chunk].settings.mesh; }
	
	public void UpdateMesh()
	{
		RecalculateNormals();
		foreach (PATileTerrainChunk c in settings.chunks)
		{
			c.settings.mesh.RecalculateBounds();
			c.gameObject.GetComponent<MeshCollider>().enabled = false;
			c.gameObject.GetComponent<MeshCollider>().enabled = true;
		}
		
	}
	
	public void UpdateMesh(List<PAPointXY> points)
	{ 		
		RecalculateNormals(points);
		foreach (PATileTerrainChunk c in settings.chunks)
		{
			c.settings.mesh.RecalculateBounds();
			c.gameObject.GetComponent<MeshCollider>().enabled = false;
			c.gameObject.GetComponent<MeshCollider>().enabled = true;
		}
	}
	
	public float GetPointHeight(int x, int y)
	{								
		PATile tile;
		Mesh mesh;
		PAPoint point = settings.points[(settings.xCount + 1) * y + x];
		for (int i = 0; i < 4; ++i)
			if (point.t[i] >= 0)
			{
				tile = GetTile(point.t[i]);
				mesh = GetChunkMesh(tile.chunkId);
				return mesh.vertices[tile.cId * 4 + point.p[i]].y;
				//return GetTile(point.t[i]).verts[point.p[i]].y;
			}	
		return 0.0f;
	}
	
	public void SetPointHeight(int x, int y, float h, bool a)
	{
		PATile tile;
		int i, j, id;
		bool c;
		List<Mesh> ms = new List<Mesh>();
		List<Vector3[]> vs = new List<Vector3[]>();
		
		Mesh mesh;
		Vector3[] vertices;
		PAPoint point = settings.points[(settings.xCount + 1) * y + x];
		for (i = 0; i < 4; ++i)
			if (point.t[i] >= 0)
			{
				c = false;
				tile = GetTile(point.t[i]);
				mesh = GetChunkMesh(tile.chunkId);
				vertices = null;
				for (j = 0; j < ms.Count; ++j) if (ms[j] == mesh) { c = true; vertices = vs[j]; }
				if (vertices == null) vertices = mesh.vertices;
			
				id = tile.cId * 4 + point.p[i];
				if (a) vertices[id].y += h; else vertices[id].y = h;
				vertices[id].y = Mathf.Clamp(vertices[id].y, settings.minHeight, settings.maxHeight);
				
				if (!c) { ms.Add(mesh); vs.Add(vertices); }
			}	
		for (i = 0; i < ms.Count; ++i) ms[i].vertices = vs[i];
	}
	
	public void SetPointNormal(int x, int y, Vector3 n)
	{
		PATile tile;
		int i, j;
		bool c;
		List<Mesh> ms = new List<Mesh>();
		List<Vector3[]> ns = new List<Vector3[]>();
		
		Mesh mesh;
		Vector3[] normals;
		PAPoint point = settings.points[(settings.xCount + 1) * y + x];
		for (i = 0; i < 4; ++i)
			if (point.t[i] >= 0)
			{
				c = false;
				tile = GetTile(point.t[i]);			
							
				mesh = GetChunkMesh(tile.chunkId);
				normals = null;
				for (j = 0; j < ms.Count; ++j) if (ms[j] == mesh) { c = true; normals = ns[j]; }
				if (normals == null) normals = mesh.normals;
			
				normals[tile.cId * 4 + point.p[i]] = n;
			
				if (!c) { ms.Add(mesh); ns.Add(normals); }
			}
			
		for (i = 0; i < ms.Count; ++i) ms[i].normals = ns[i];
	}
	
	public void RecalculateNormals(List<PAPointXY> points)
	{
		for (int i = 0; i < points.Count; ++i) RecalculatePointNormal(points[i].x, points[i].y);
	}
	
	public void RecalculateNormals()
	{
		for (int y = 0; y <= settings.yCount; ++y)	
			for (int x = 0; x <= settings.xCount; ++x) RecalculatePointNormal(x, y);		
	}
	
	public void RecalculatePointNormal(int x, int y)
	{		
	    Vector3 v0, v1;
	    Vector3 n0, n1, n2, n3, n4, n5;
				
	    //(0, -1)
	    if (y > 0) v0 = new Vector3(0.0f, GetPointHeight(x, y - 1) - GetPointHeight(x, y), -settings.tileSize);
	    else v0 = new Vector3(0.0f, 0.0f, -settings.tileSize);
		
	    //(-1, -1)
	    if(x > 0 && y > 0) v1 = new Vector3(-settings.tileSize, GetPointHeight(x - 1, y - 1) - GetPointHeight(x, y), -settings.tileSize);
	    else v1 = new Vector3(-settings.tileSize, 0.0f, -settings.tileSize);
	    n0 = Vector3.Cross(v0, v1);
	
	    //(-1, -1)
	    v0 = v1;
	    //(-1, 0)
	    if (x > 0) v1 = new Vector3(-settings.tileSize, GetPointHeight(x - 1, y) - GetPointHeight(x, y), 0.0f);
	    else v1 = new Vector3(-settings.tileSize, 0.0f, 0.0f);
	    n1 = Vector3.Cross(v0, v1);
	
	    //(-1, 0)
	    v0 = v1;
	    //(0, 1)
	    if (y < settings.yCount) v1 = new Vector3(0.0f, GetPointHeight(x, y + 1) - GetPointHeight(x, y), settings.tileSize);
	    else v1 = new Vector3(0.0f, 0.0f, settings.tileSize);
	    n2 = Vector3.Cross(v0, v1);
	
	    //(0, 1)
	    v0 = v1;
	    //(1, 1)
	    if (x < settings.xCount && y < settings.yCount) v1 = new Vector3(settings.tileSize, GetPointHeight(x + 1, y + 1) - GetPointHeight(x, y), settings.tileSize);
	    else v1 = new Vector3(settings.tileSize, 0.0f, settings.tileSize);
	    n3 = Vector3.Cross(v0, v1);
	
	    //(1, 1)
	    v0 = v1;
	    //(1, 0)
	    if (x < settings.xCount) v1 = new Vector3(settings.tileSize, GetPointHeight(x + 1, y) - GetPointHeight(x, y), 0.0f);
	    else v1 = new Vector3(settings.tileSize, 0.0f, 0.0f);
	    n4 = Vector3.Cross(v0, v1);
	
	    //(1, 0)
	    v0 = v1;
	    //(0, -1)
	    if (y > 0) v1 = new Vector3(0.0f, GetPointHeight(x, y - 1) - GetPointHeight(x, y), -settings.tileSize);
	    else v1 = new Vector3(0.0f, 0.0f, -settings.tileSize);
	    n5 = Vector3.Cross(v0, v1);
	
	    Vector3 m0, m1, m2, m3;
	    m0 = (n1 - n0) / 2 + n0;
	    m1 = n2;
	    m2 = (n4 - n3) / 2 + n3;
	    m3 = n5;
		
	    Vector3 k0, k1;
	    k0 = (m2 - m0) / 2 + m0;
	    k1 = (m3 - m1) / 2 + m1;
	
	    Vector3 n = (k1 - k0) / 2 + k0;
	    n = n.normalized;
		
		SetPointNormal(x, y, n);
	}
	
	public void SetVertexColors(int x, int y, bool rect, float power, Color clr, float radius, List<PAPointXY> points)
	{
		int i, j;
		int count = (int)(radius / settings.tileSize);
		int xMin = x, xMax = x, yMin = y, yMax = y;
		//float distance;
		Vector3 currentPos, tilePos;
		PAPointXY p;
		
		xMin = x - count; xMin = Mathf.Clamp(xMin, 0, settings.xCount);
		yMin = y - count; yMin = Mathf.Clamp(yMin, 0, settings.yCount);
		xMax = x + count; xMax = Mathf.Clamp(xMax, 0, settings.xCount);
		yMax = y + count; yMax = Mathf.Clamp(yMax, 0, settings.yCount);
		
		currentPos = new Vector3(x * settings.tileSize, 0.0f, y * settings.tileSize);	
		
		for (j = yMin; j <= yMax; ++j)	
		for (i = xMin; i <= xMax; ++i) 
		{
			tilePos = new Vector3(i * settings.tileSize, 0.0f, j * settings.tileSize);
			
			if (!rect && Vector3.Distance(tilePos, currentPos) > radius) continue;
			p = new PAPointXY(i, j); if (!points.Contains(p)) points.Add(p);
			SetVertexColor(i, j, power, clr);
		}
	}
	
	protected void SetVertexColor(int x, int y, float power, Color clr)
	{
		PATile tile;
		int i, j, id;
		bool c;
		List<Mesh> ms = new List<Mesh>();
		List<Color[]> clrs = new List<Color[]>();
		
		Mesh mesh;
		Color[] colors;
		
		PAPoint point = settings.points[(settings.xCount + 1) * y + x];
		for (i = 0; i < 4; ++i)
			if (point.t[i] >= 0)
			{
				c = false;
				tile = GetTile(point.t[i]);
			
				mesh = GetChunkMesh(tile.chunkId);
				colors = null;
			
				for (j = 0; j < ms.Count; ++j) if (ms[j] == mesh) { c = true; colors = clrs[j]; }
				if (colors == null) colors = mesh.colors;		
				
				id = tile.cId * 4 + point.p[i];
				colors[id] = Color.Lerp(colors[id], clr, power);
				
				if (!c) { ms.Add(mesh); clrs.Add(colors); }
			}	
		for (i = 0; i < ms.Count; ++i) ms[i].colors = clrs[i];
	}
	
	public void SmoothPointTerrain(int x, int y, bool rect, float power, float radius, List<PAPointXY> points)
	{		
		int i, j, pi, mi;
		int count = (int)(radius / settings.diagonalLength);
		int xMin = x, xMax = x, yMin = y, yMax = y;
		float h;
		Vector3 currentPos, tilePos;
		PAPointXY p;
		PATile tile;
		List<Mesh> ms = new List<Mesh>();
		List<Vector3[]> vs = new List<Vector3[]>();
		Mesh mesh;
		bool c;
		PAPoint point;
		Vector3[] vertices;		
		
		xMin = x - count; xMin = Mathf.Clamp(xMin, 0, settings.xCount);
		yMin = y - count; yMin = Mathf.Clamp(yMin, 0, settings.yCount);
		xMax = x + count; xMax = Mathf.Clamp(xMax, 0, settings.xCount);
		yMax = y + count; yMax = Mathf.Clamp(yMax, 0, settings.yCount);
		
		currentPos = new Vector3(x * settings.tileSize, 0.0f, y * settings.tileSize);	
		
		for (j = yMin; j <= yMax; ++j)	
		for (i = xMin; i <= xMax; ++i) 
		{
			tilePos = new Vector3(i * settings.tileSize, 0.0f, j * settings.tileSize);
			
			if (!rect && Vector3.Distance(tilePos, currentPos) > radius) continue;
			p = new PAPointXY(i, j); if (!points.Contains(p)) points.Add(p);
			h = GetSmoothPointHeight(i, j, power);
			
			point = settings.points[(settings.xCount + 1) * j + i];
			for (pi = 0; pi < 4; ++pi)
				if (point.t[pi] >= 0)
				{
					c = false;
					tile = GetTile(point.t[pi]);
					mesh = GetChunkMesh(tile.chunkId);
					vertices = null;
					
					for (mi = 0; mi < ms.Count; ++mi) if (ms[mi] == mesh) { c = true; vertices = vs[mi]; }
					if (vertices == null) vertices = mesh.vertices;
				
					vertices[tile.cId * 4 + point.p[pi]].y = Mathf.Clamp(h, settings.minHeight, settings.maxHeight);
	
					if (!c) { ms.Add(mesh); vs.Add(vertices); }
				}	
		}
		
		for (i = 0; i < ms.Count; ++i) ms[i].vertices = vs[i];
	}
	
	public float GetSmoothPointHeight(int x, int y, float p)
	{			
		PAPoint point = settings.points[(settings.xCount + 1) * y + x];
		PATile tile = GetTile(point.t[0]);		
		//float h = GetTile(point.t[0]).verts[point.p[0]].y;
		float h = GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y;
		float hh = h, hs = h, hr, hp;
		int hc = 1;		
		
		if (x > 0)  //L
		{			
			point = settings.points[(settings.xCount + 1) * y + x - 1];
			tile = GetTile(point.t[0]);
			h += GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y; hc++;
			//h += GetTile(point.t[0]).verts[point.p[0]].y; hc++;
		}	
		if (y < settings.yCount) //U
		{
			point = settings.points[(settings.xCount + 1) * (y + 1) + x];
			tile = GetTile(point.t[0]);
			h += GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y; hc++;
			//h += GetTile(point.t[0]).verts[point.p[0]].y; hc++;
		}
		if (x < settings.xCount) //R
		{
			point = settings.points[(settings.xCount + 1) * y + x + 1];
			tile = GetTile(point.t[0]);
			h += GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y; hc++;
			//h += GetTile(point.t[0]).verts[point.p[0]].y; hc++;
		}			
		if (y > 0) //D
		{
			point = settings.points[(settings.xCount + 1) * (y - 1) + x];
			tile = GetTile(point.t[0]);
			h += GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y; hc++;
			//h += GetTile(point.t[0]).verts[point.p[0]].y; hc++;
		}
		if (x > 0 && y < settings.yCount) //LU
		{
			point = settings.points[(settings.xCount + 1) * (y + 1) + x - 1];
			tile = GetTile(point.t[0]);
			h += GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y; hc++;
			//h += GetTile(point.t[0]).verts[point.p[0]].y; hc++;
		}
		if (x > 0 && y > 0) //LD
		{
			point = settings.points[(settings.xCount + 1) * (y - 1) + x - 1];
			tile = GetTile(point.t[0]);
			h += GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y; hc++;
			//h += GetTile(point.t[0]).verts[point.p[0]].y; hc++;
		}
		if (x < settings.xCount && y < settings.yCount) //RU
		{
			point = settings.points[(settings.xCount + 1) * (y + 1) + x + 1];
			tile = GetTile(point.t[0]);
			h += GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y; hc++;
			//h += GetTile(point.t[0]).verts[point.p[0]].y; hc++;
		}
		if (x < settings.xCount && y > 0) //RD
		{
			point = settings.points[(settings.xCount + 1) * (y - 1) + x + 1];
			tile = GetTile(point.t[0]);
			h += GetChunkMesh(tile.chunkId).vertices[tile.cId * 4 + point.p[0]].y; hc++;
			//h += GetTile(point.t[0]).verts[point.p[0]].y; hc++;
		}		
		
		hr = h / hc;		
		if (hs > hr) hh = hs - hr;
		else hh = hr - hs;
		if (hs > hr) hp = hs - p * hh;
		else  hp = hs + p * hh;			
		
		//SetPointHeight(x, y, hp, false);	
		return hp;
	}	
	
	public void DeformPointTerrain(int x, int y, bool rect, float power, float radius, List<PAPointXY> points)
	{		
		int count = (int)(radius / settings.diagonalLength);
		int xMin = x, xMax = x, yMin = y, yMax = y;
		int i, j;
		float distance, falloff;
		Vector3 currentPos, tilePos;
		PAPointXY p;		
		
		xMin = x - count; xMin = Mathf.Clamp(xMin, 0, settings.xCount);
		yMin = y - count; yMin = Mathf.Clamp(yMin, 0, settings.yCount);
		xMax = x + count; xMax = Mathf.Clamp(xMax, 0, settings.xCount);
		yMax = y + count; yMax = Mathf.Clamp(yMax, 0, settings.yCount);
	
			
		currentPos = new Vector3(x * settings.tileSize, 0.0f, y * settings.tileSize);	
		for (j = yMin; j <= yMax; ++j)	
		for (i = xMin; i <= xMax; ++i) 
		{
			p = new PAPointXY(i, j); if (!points.Contains(p)) points.Add(p);			
			if (rect)
			{
				falloff = power;
			} else
			{
				tilePos = new Vector3(i * settings.tileSize, 0.0f, j * settings.tileSize);
				distance = Vector3.Distance(currentPos, tilePos);
				falloff = GaussFalloff(distance, radius) * power;
			}
			SetPointHeight(i, j, falloff, true);
		}	
			
	}
	
	//Painting	
	protected PATileUV GetIndexUV(int index)
	{
		PATileUV uv = new PATileUV();
		
		int x, y;
		
		y = index / settings.tilesetX;
		x = index - y * settings.tilesetX;
		y = settings.tilesetY - y - 1; //(0,0) in left-bottom	
		
		uv.p0 = new Vector2(x * settings.tilesetWidth + settings.uvOffset.x, 
		                    y * settings.tilesetHeight + settings.tilesetHeight - settings.uvOffset.y); 
		uv.p1 = new Vector2(x * settings.tilesetWidth + settings.tilesetWidth -settings.uvOffset.x, 
		                    y * settings.tilesetHeight + settings.tilesetHeight - settings.uvOffset.y); 
		uv.p2 = new Vector2(x * settings.tilesetWidth + settings.tilesetWidth - settings.uvOffset.x, 
		                    y * settings.tilesetHeight + settings.uvOffset.y);  
		uv.p3 = new Vector2(x * settings.tilesetWidth + settings.uvOffset.x, 
		                    y * settings.tilesetHeight + settings.uvOffset.y);
		return uv;
	}
	
	public PATile[] GetNeighboringTilesNxN(PATile tile, int n) //n must be 1,3,5,7,9,11... etc
	{ return GetNeighboringTilesNxN(tile.x, tile.y, n); }
	
	public PATile[] GetNeighboringTilesNxN(int x, int y, int n) //n must be 1,3,5,7,9,11... etc
	{
		//Universal algorithm to search for nearby tiles
		
		//n = 1 ---> 1x1 - 8
		//Array visualization, where 'x' = current tile
		// 0  1  2
		// 7  x  3
		// 6  5  4
			
		//n = 3 ---> 3x3 - 16
		//Array visualization, where 'c' = current tile
		// 0  1  2  3  4
		//15  x  x  x  5
		//14  x  c  x  6
		//13  x  x  x  7 
		//12 11 10  9  8 	
		
		//n = 5 ---> 5x5 - 24
		//Array visualization, where 'c' = current tile
		// 0  1  2  3  4  5  6
		// 23 x  x  x  x  x  7
		// 22 x  x  x  x  x  8
		// 21 x  x  c  x  x  9
		// 20 x  x  x  x  x  10
		// 19 x  x  x  x  x  11
		// 18 17 16 15 14 13 12
		
		int i;
		int nc = n + 2;
		int ct = nc * nc - n * n; 
		int m = (nc - 1) / 2, s;
		
		PATile[] t = new PATile[ct]; 
		for (i = 0; i < ct; ++i) t[i] = null;
		
		//up
		if (x > (m - 1)) 
		{
			//left and center
			for (i = -1; i < m; ++i)
				if (y > i) t[m - (i + 1)] = GetTile(x - m, y - (i + 1));
			//right
			for (i = 1; i <= m; ++i)
				if (y < settings.yCount - i) t[m + i] = GetTile(x - m, y + i);
		}
		
		//bottom
		if (x < settings.xCount - m)
		{
			//right and center
			s = nc + n;
			for (i = m; i >= 0; --i) 
				if (y < settings.yCount - i) t[s + m - i] = GetTile(x + m, y + i);
			//left
			s += m + 1;
			for (i = 0; i < m; ++i)
				if (y > i) t[s + i] = GetTile(x + m, y - (i + 1));
		}
		
		//right
		if (y < settings.yCount - m)
		{
			//up
			for (i = m - 2; i >= -1; --i) 
				if (x > i) t[n + m - i]	= GetTile(x - (i + 1), y + m);
			//bottom
			for (i = 1; i < m; ++i) 
				if (x < settings.xCount - i) t[nc + m + i - 1] = GetTile(x + i, y + m);
		}
		
		//left
		if (y > m - 1)
		{
			//bottom
			s = ct - n;
			for (i = m - 1; i >= 0; --i)
				if (x < settings.xCount - i) t[s + m - 1 - i] = GetTile(x + i, y - m);
			//up
			s = ct - m + 1;
			for (i = 0; i < m - 1; ++i)
				if (x > i) t[s + i] = GetTile(x - (i + 1), y - m);
		} 
		
		return t;
	}
	
	/*public PATile[] GetNeighboringTiles1x1(PATile tile)
	{
		//Array visualization, where 'x' = current tile
		// 0  1  2
		// 7  x  3
		// 6  5  4
		PATile[] t = new PATile[8]; 
		for (int i = 0; i < 8; ++i) t[i] = null;

		if (tile.x > 0) //0, 1, 2
		{
			//0
			if (tile.y > 0) t[0] = GetTile(tile.x - 1, tile.y - 1);
			//1
			t[1] = GetTile(tile.x - 1, tile.y);
			//2
			if (tile.y < settings.yCount - 1) t[2] = GetTile(tile.x - 1, tile.y + 1);
		}
		
		if (tile.x < settings.xCount - 1) //4, 5, 6
		{
			//4
			if (tile.y < settings.yCount - 1) t[4] = GetTile(tile.x + 1, tile.y + 1);	
			//5
			t[5] = GetTile(tile.x + 1, tile.y);
			//6
			if (tile.y > 0) t[6] = GetTile(tile.x + 1, tile.y - 1);	
		}
		
		//7
		if (tile.y > 0) t[7] = GetTile(tile.x, tile.y - 1);
		//3
		if (tile.y < settings.yCount - 1) t[3] = GetTile(tile.x, tile.y + 1);
		
		for (int i = 0; i < 8; ++i) CheckTile(t[i]);
		return t;
	}
	
	public PATile[] GetNeighboringTiles3x3(PATile tile)
	{
		//Array visualization, where 'c' = current tile
		// 0  1  2  3  4
		//15  x  x  x  5
		//14  x  c  x  6
		//13  x  x  x  7 
		//12 11 10  9  8 
		PATile[] t = new PATile[16]; 
		for (int i = 0; i < 16; ++i) t[i] = null;
		
		if (tile.x > 1) //0, 1, 2, 3, 4
		{
			//0
			if (tile.y > 1) t[0] = GetTile(tile.x - 2, tile.y - 2);
			//1
			if (tile.y > 0) t[1] = GetTile(tile.x - 2, tile.y - 1);
			//2
			t[2] = GetTile(tile.x - 2, tile.y);
			//3
			if (tile.y < settings.yCount - 1) t[3] = GetTile(tile.x - 2, tile.y + 1);
			//4
			if (tile.y < settings.yCount - 2) t[4] = GetTile(tile.x - 2, tile.y + 2);				
		}
		
		if (tile.x < settings.xCount - 2) //12, 11, 10, 9, 8
		{
			//8
			if (tile.y < settings.yCount - 2) t[8] = GetTile(tile.x + 2, tile.y + 2);
			//9
			if (tile.y < settings.yCount - 1) t[9] = GetTile(tile.x + 2, tile.y + 1);
			//10
			t[10] = GetTile(tile.x + 2, tile.y);
			//11
			if (tile.y > 0) t[11] = GetTile(tile.x + 2, tile.y - 1);
			//12
			if (tile.y > 1) t[12] = GetTile(tile.x + 2, tile.y - 2);
		}
		
		//5, 6, 7
		if (tile.y < settings.yCount - 2)
		{
			//5
			if (tile.x > 0) t[5] = GetTile(tile.x - 1, tile.y + 2);
			//6
			t[6] = GetTile(tile.x, tile.y + 2);
			//7
			if (tile.x < settings.xCount - 1) t[7] = GetTile(tile.x + 1, tile.y + 2);
		}	
		
		//13, 14, 15
		if (tile.y > 1)
		{
			//13
			if (tile.x < settings.xCount - 1) t[13] = GetTile(tile.x + 1, tile.y - 2);
			//14
			t[14] = GetTile(tile.x, tile.y - 2);
			//15
			if (tile.x > 0) t[15] = GetTile(tile.x - 1, tile.y - 2);
		}	
		
		return t;
	}
	
	public PATile[] GetNeighboringTiles5x5(PATile tile)
	{
		//Array visualization, where 'c' = current tile
		// 0  1  2  3  4  5  6
		// 23 x  x  x  x  x  7
		// 22 x  x  x  x  x  8
		// 21 x  x  c  x  x  9
		// 20 x  x  x  x  x  10
		// 19 x  x  x  x  x  11
		// 18 17 16 15 14 13 12
		PATile[] t = new PATile[24]; 
		for (int i = 0; i < 24; ++i) t[i] = null;
		
		if (tile.x > 2) //0, 1, 2, 3, 4, 5, 6
		{
			//0
			if (tile.y > 2) t[0] = GetTile(tile.x - 3, tile.y - 3);
			//1
			if (tile.y > 1) t[1] = GetTile(tile.x - 3, tile.y - 2);
			//2
			if (tile.y > 0) t[2] = GetTile(tile.x - 3, tile.y - 1);
			//3
			t[3] = GetTile(tile.x - 3, tile.y);
			//4
			if (tile.y < settings.yCount - 1) t[4] = GetTile(tile.x - 3, tile.y + 1);
			//5
			if (tile.y < settings.yCount - 2) t[5] = GetTile(tile.x - 3, tile.y + 2);
			//6
			if (tile.y < settings.yCount - 3) t[6] = GetTile(tile.x - 3, tile.y + 3);				
		}
		
		if (tile.x < settings.xCount - 3) //12, 13, 14, 15, 16, 17, 18
		{
			//12
			if (tile.y < settings.yCount - 3) t[12] = GetTile(tile.x + 3, tile.y + 3);
			//13
			if (tile.y < settings.yCount - 2) t[13] = GetTile(tile.x + 3, tile.y + 2);
			//14
			if (tile.y < settings.yCount - 1) t[14] = GetTile(tile.x + 3, tile.y + 1);
			//15
			t[15] = GetTile(tile.x + 3, tile.y);
			//16
			if (tile.y > 0) t[16] = GetTile(tile.x + 3, tile.y - 1);
			//17
			if (tile.y > 1) t[17] = GetTile(tile.x + 3, tile.y - 2);
			//18
			if (tile.y > 2) t[18] = GetTile(tile.x + 3, tile.y - 3);
		}
		
		//Array visualization, where 'c' = current tile
		// 0  1  2  3  4  5  6
		// 23 x  x  x  x  x  7
		// 22 x  x  x  x  x  8
		// 21 x  x  c  x  x  9
		// 20 x  x  x  x  x  10
		// 19 x  x  x  x  x  11
		// 18 17 16 15 14 13 12
		
		//7, 8, 9, 10, 11
		if (tile.y < settings.yCount - 3)
		{
			//7
			if (tile.x > 1) t[7] = GetTile(tile.x - 2, tile.y + 3);
			//8
			if (tile.x > 0) t[8] = GetTile(tile.x - 1, tile.y + 3);
			//9
			t[9] = GetTile(tile.x, tile.y + 3);
			//10
			if (tile.x < settings.xCount - 1) t[10] = GetTile(tile.x + 1, tile.y + 3);
			//11
			if (tile.x < settings.xCount - 2) t[11] = GetTile(tile.x + 2, tile.y + 3);
		}	
		
		//19, 20, 21, 22, 23
		if (tile.y > 2)
		{
			//19
			if (tile.x < settings.xCount - 2) t[19] = GetTile(tile.x + 2, tile.y - 3);
			//20
			if (tile.x < settings.xCount - 1) t[20] = GetTile(tile.x + 1, tile.y - 3);
			//21
			t[21] = GetTile(tile.x, tile.y - 3);
			//22
			if (tile.x > 0) t[22] = GetTile(tile.x - 1, tile.y - 3);
			//23
			if (tile.x > 1) t[23] = GetTile(tile.x - 2, tile.y - 3);
		}	
		
		return t;
	}*/
	
	protected void CheckTile(PATile tile)
	{
		if (tile != null && tile.type >= settings.tsTypes.Count) { tile.type = -1; tile.bits = 0; }
	}
	
	public void PaintTile3x3(PATile tile, int t)
	{
		if (tile == null) return;
		int i;
		t = Mathf.Clamp(t, 0, settings.tsTypes.Count);
	 
		IntermediateInfo[] imInfo = new IntermediateInfo[16];
		PATile[] tile2 = GetNeighboringTilesNxN(tile, 1);
		PATile[] nTiles = GetNeighboringTilesNxN(tile, 3);
		PATile[] tls = GetNeighboringTilesNxN(tile, 5);
		
		tile.type = t;
		tile.toType = t;
		tile.bits = 0;
		UpdateTileUV(tile);

		for (i = 0; i < 8; ++i) 
			if (tile2[i] != null) 
			{ 
				tile2[i].type = t;
				tile2[i].toType = t;
				tile2[i].bits = 0; 	
				UpdateTileUV(tile2[i]);
			}
		
		//Array visualization, where 'c' = current tile
		// 0  1  2  3  4  5  6
		// 23 0  1  2  3  4  7
		// 22 15 x  x  x  5  8
		// 21 14 x  c  x  6  9
		// 20 13 x  x  x  7  10
		// 19 12 11 10 9  8  11
		// 18 17 16 15 14 13 12
		
		//0	
		CalcTileBits(t, nTiles[0], 2, out imInfo[0]);
		//1
		CalcTileBits(t, nTiles[1], 3, out imInfo[1]);		
		//2
		CalcTileBits(t, nTiles[2], 3, out imInfo[2]);
		//3
		CalcTileBits(t, nTiles[3], 3, out imInfo[3]);
		//4
		CalcTileBits(t, nTiles[4], 1, out imInfo[4]);
		
		//5
		CalcTileBits(t, nTiles[5], 9, out imInfo[5]);
		//6
		CalcTileBits(t, nTiles[6], 9, out imInfo[6]);
		//7
		CalcTileBits(t, nTiles[7], 9, out imInfo[7]);
		
		//8	
		CalcTileBits(t, nTiles[8], 8, out imInfo[8]);
		//9
		CalcTileBits(t, nTiles[9], 12, out imInfo[9]);		
		//10
		CalcTileBits(t, nTiles[10], 12, out imInfo[10]);
		//11
		CalcTileBits(t, nTiles[11], 12, out imInfo[11]);
		//12
		CalcTileBits(t, nTiles[12], 4, out imInfo[12]);
		
		//13
		CalcTileBits(t, nTiles[13], 6, out imInfo[13]);
		//14
		CalcTileBits(t, nTiles[14], 6, out imInfo[14]);
		//15
		CalcTileBits(t, nTiles[15], 6, out imInfo[15]);
	
		//Array visualization, where 'c' = current tile
		// 0  1  2  3  4  5  6
		// 23 0  1  2  3  4  7
		// 22 15 x  x  x  5  8
		// 21 14 x  c  x  6  9
		// 20 13 x  x  x  7  10
		// 19 12 11 10 9  8  11
		// 18 17 16 15 14 13 12
		
		//0	
		CalcIntermediateTileBits(tls[0], 2, imInfo[0]);
		//1
		CalcIntermediateTileBits(tls[1], 3, imInfo[0]);
		//2
		CalcIntermediateTileBits(tls[2], 3, imInfo[1]);
		//3
		CalcIntermediateTileBits(tls[3], 3, imInfo[2]);
		//4
		CalcIntermediateTileBits(tls[4], 3, imInfo[3]);
		//5
		CalcIntermediateTileBits(tls[5], 3, imInfo[4]);
		//6
		CalcIntermediateTileBits(tls[6], 1, imInfo[4]);
		
		//7
		CalcIntermediateTileBits(tls[7], 9, imInfo[4]);
		//8
		CalcIntermediateTileBits(tls[8], 9, imInfo[5]);
		//9
		CalcIntermediateTileBits(tls[9], 9, imInfo[6]);	
		//10
		CalcIntermediateTileBits(tls[10], 9, imInfo[7]);
		//11
		CalcIntermediateTileBits(tls[11], 9, imInfo[8]);
		
		//12	
		CalcIntermediateTileBits(tls[12], 8, imInfo[8]);
		//13
		CalcIntermediateTileBits(tls[13], 12, imInfo[8]);		
		//14
		CalcIntermediateTileBits(tls[14], 12, imInfo[9]);
		//15
		CalcIntermediateTileBits(tls[15], 12, imInfo[10]);
		//16
		CalcIntermediateTileBits(tls[16], 12, imInfo[11]);
		//17
		CalcIntermediateTileBits(tls[17], 12, imInfo[12]);
		//18
		CalcIntermediateTileBits(tls[18], 4, imInfo[12]);
		
		//19
		CalcIntermediateTileBits(tls[19], 6, imInfo[12]);
		//20
		CalcIntermediateTileBits(tls[20], 6, imInfo[13]);
		//21
		CalcIntermediateTileBits(tls[21], 6, imInfo[14]);
		//22
		CalcIntermediateTileBits(tls[22], 6, imInfo[15]);			
		//23
		CalcIntermediateTileBits(tls[23], 6, imInfo[0]);
	}
		
	public void PaintTile1x1(PATile tile, int t)
	{
		if (tile == null) return;
		t = Mathf.Clamp(t, 0, settings.tsTypes.Count);
		
		//int i, transitionId;
		//PATSTransition transition = FindTransitionForType(t, out transitionId);
		//if (transition == null) return;
		
		IntermediateInfo[] imInfo = new IntermediateInfo[8];
		PATile[] nTiles = GetNeighboringTilesNxN(tile, 1);
		PATile[] tls = GetNeighboringTilesNxN(tile, 3);
		
		//Current tile		
		tile.type = t;
		tile.toType = t;
		tile.bits = 0;
		UpdateTileUV(tile);
		
		//0	
		CalcTileBits(t, nTiles[0], 2, out imInfo[0]);
		//1
		CalcTileBits(t, nTiles[1], 3, out imInfo[1]);		
		//2
		CalcTileBits(t, nTiles[2], 1, out imInfo[2]);
		//3
		CalcTileBits(t, nTiles[3], 9, out imInfo[3]);
		//4
		CalcTileBits(t, nTiles[4], 8, out imInfo[4]);
		//5
		CalcTileBits(t, nTiles[5], 12, out imInfo[5]);
		//6
		CalcTileBits(t, nTiles[6], 4, out imInfo[6]);
		//7
		CalcTileBits(t, nTiles[7], 6, out imInfo[7]);
		
		//Array visualization, where 'c' = current tile
		// 0  1  2  3  4
		//15  0  1  2  5
		//14  7  c  3  6
		//13  6  5  4  7 
		//12 11 10  9  8 	
		
		//0	
		CalcIntermediateTileBits(tls[0], 2, imInfo[0]);
		//1
		CalcIntermediateTileBits(tls[1], 3, imInfo[0]);
		//2
		CalcIntermediateTileBits(tls[2], 3, imInfo[1]);
		//3
		CalcIntermediateTileBits(tls[3], 3, imInfo[2]);
		//4
		CalcIntermediateTileBits(tls[4], 1, imInfo[2]);	
		
		//5
		CalcIntermediateTileBits(tls[5], 9, imInfo[2]);
		//6
		CalcIntermediateTileBits(tls[6], 9, imInfo[3]);
		//7
		CalcIntermediateTileBits(tls[7], 9, imInfo[4]);	
		
		//8	
		CalcIntermediateTileBits(tls[8], 8, imInfo[4]);
		//9
		CalcIntermediateTileBits(tls[9], 12, imInfo[4]);		
		//10
		CalcIntermediateTileBits(tls[10], 12, imInfo[5]);
		//11
		CalcIntermediateTileBits(tls[11], 12, imInfo[6]);
		//12
		CalcIntermediateTileBits(tls[12], 4, imInfo[6]);
		
		//13
		CalcIntermediateTileBits(tls[13], 6, imInfo[6]);
		//14
		CalcIntermediateTileBits(tls[14], 6, imInfo[7]);
		//15
		CalcIntermediateTileBits(tls[15], 6, imInfo[0]);
	}
	
	protected void CalcIntermediateTileBits(PATile tile, byte b, IntermediateInfo imInfo)
	{
		if (tile == null || imInfo == null) return;
		
		IntermediateInfo imInfoInternal;		
		CalcTileBits(imInfo.imToType, tile, b, out imInfoInternal);
	}
	
	protected void CalcTileBits(int t, PATile tile, byte b, out IntermediateInfo imInfo)
	{
		imInfo = null;
		if (tile == null) return;
		
		PATSTransition transition = null;
		bool invertedBits, needIntermediate = false;
		byte bits = tile.bits;
		bool isNull = (bits == 0 || tile.type == tile.toType);
		int fromType = t, toType = t;	
		
		if (isNull)
		{			
			if (fromType != tile.type)
			{
				transition = FindTransition(fromType, tile.type);
				toType = tile.type;
				if (transition != null)
				{
					invertedBits = (fromType == transition.to);
					bits = b;
					if (invertedBits) bits = InvertBits(bits);	
					
				} else 
				{
					needIntermediate = true;
				}
			} else if (fromType == tile.type)
			{
				//nothing to do
				toType = fromType; bits = 0;
			}
			
		} else
		{
			if (fromType == tile.type) 
			{
				transition = FindTransition(fromType, tile.toType);
				toType = tile.toType;
			}
			else if (fromType == tile.toType) 
			{
				transition = FindTransition(fromType, tile.type);
				toType = tile.type;
			} else 
			{
				needIntermediate = true;
				toType = tile.type;
			}
			
			if (transition != null)
			{
				invertedBits = (fromType == transition.to);
				
				//if (fromType == tile.type)
					// -----       -----
					// |1|2|  ---  | 1 |
					// -----       -----
				//else if (fromType == tile.toType)
					// -----       -----
					// |2|1|  ---  | 1 |
					// -----       -----				
				
				if (invertedBits) bits = InvertBits(bits);
				bits = (byte)(bits | b);
				if (invertedBits) bits = InvertBits(bits);
				if (bits == 15) { bits = 0; toType = fromType; }
			}
					
		}
		
		//Intermediate transition
		if (needIntermediate)
		{
			imInfo = new IntermediateInfo();
			imInfo.fromType = fromType;
			imInfo.toType = toType;
			
			transition = FindIntermediateTransition(fromType, toType);
			if (transition != null)
			{
				//Debug.Log("Intermediate transition = " + transition.name);
				if (fromType == transition.from) toType = transition.to;
				else if (fromType == transition.to) toType = transition.from;
					
				invertedBits = (fromType == transition.to);
				bits = b;
				if (invertedBits) bits = InvertBits(bits);
				if (bits == 15) { bits = 0; toType = fromType; }
				
				imInfo.imFromType = fromType;
				imInfo.imToType = toType;
			} else 
			{
				Debug.LogError("Not found transition between '"+ settings.tsTypes[fromType].name +"' and '" + settings.tsTypes[toType].name + "'!");
				imInfo = null;
			}
		}			
		
		tile.type = fromType;
		tile.toType = toType;
		tile.bits = bits;
		UpdateTileUV(tile);
	}
	
	protected void UpdateTileUV(PATile tile)
	{
		if (tile.type == -1) { return; }	

		Mesh mesh = GetChunkMesh(tile.chunkId);
		Vector2[] uvs = mesh.uv;
		int i = tile.cId;
		int index;

		if (tile.bits == 0 || tile.type == tile.toType)
		{
			PATSType type = settings.tsTypes[tile.type];
			index = type.baseIndexes[0];
			tile.bits = 0;
		} else
		{			
			int id = FindTransitionBitsId(tile.bits);
			int transitionId;			
			
			PATSTransition transition = FindTransition(tile.type, tile.toType, out transitionId);	
			if (transition == null)
			{
				Debug.LogError("For the tile set is not known transition!");
				PATSType type = settings.tsTypes[tile.type];
				index = type.baseIndexes[0];
			} else 
			{
				index = transition.transitions[id];
			}
		}				
		
		PATileUV uv = GetIndexUV(index);
		//tile.uvs[0] = uv.p0;
		//tile.uvs[1] = uv.p1;
		//tile.uvs[2] = uv.p2;
		//tile.uvs[3] = uv.p3;		
		uvs[i * 4 + 0] = uv.p0;//tile.uvs[0];
		uvs[i * 4 + 1] = uv.p1;//tile.uvs[1];
		uvs[i * 4 + 2] = uv.p2;//tile.uvs[2];
		uvs[i * 4 + 3] = uv.p3;//tile.uvs[3];
		
		mesh.uv = uvs;
	}
	
	public void FillTerrain(int t)
	{		
		PATile tile;
		PATSType type = settings.tsTypes[t];
		//int transition = FindTransitionId(t);
		PATileUV uv = GetIndexUV(type.baseIndexes[0]);
		
		Mesh mesh;
		Vector2[] uvs;
		for (int i = 0; i < settings.xCount * settings.yCount; ++i)
		{
			tile = settings.tiles[i];
			mesh = GetChunkMesh(tile.chunkId);
			uvs = mesh.uv;
			
			tile.type = t;
			tile.toType = t;
			tile.bits = 0;	
			
			//tile.uvs[0] = uv.p0;
			//tile.uvs[1] = uv.p1;
			//tile.uvs[2] = uv.p2;
			//tile.uvs[3] = uv.p3;	
			uvs[tile.cId * 4 + 0] = uv.p0;//tile.uvs[0];
			uvs[tile.cId * 4 + 1] = uv.p1;//tile.uvs[1];
			uvs[tile.cId * 4 + 2] = uv.p2;//tile.uvs[2];
			uvs[tile.cId * 4 + 3] = uv.p3;//tile.uvs[3];
			
			mesh.uv = uvs;
		}
	}
		
	public void CreateTerrain()
	{
		DestroyTerrain();
		
		settings.name = editorSettings.name;
		settings.xCount = editorSettings.x;
		settings.yCount = editorSettings.y;
		settings.chunkSize = editorSettings.chunkSize;
		settings.tileSize = editorSettings.tileSize;
		settings.minHeight = editorSettings.minHeight;
		settings.maxHeight = editorSettings.maxHeight;
		settings.diagonalLength = Mathf.Sqrt(Mathf.Pow(editorSettings.tileSize, 2) + Mathf.Pow(editorSettings.tileSize, 2));
		
		settings.tilesetX = 1;
		settings.tilesetY = 1;
		settings.tilesetCount = 1;
		settings.tilesetWidth = 1.0f;
		settings.tilesetHeight = 1.0f;
		settings.tilesetMaterial = editorSettings.tileSetMaterial;
		
		List<Vector3> verts;
		List<int> tris;
		List<Vector2> uvs;
		List<Color> colors;
		List<Vector3> normals;
		
		PATile tile;
		PAPoint point;
		int x, y, li, i, cx, cy, cId, sx, sy;
		int w = settings.xCount, 
			h = settings.yCount; 
		int cw, ch;
		int chunkCountX, chunkCountY;
		float tileSize = settings.tileSize;
		GameObject go;
		string str;
		Vector3 tpos = transform.position;
		PATileTerrainChunk chunk;
		
		Mesh mesh;
		MeshCollider meshCollider;
		MeshFilter meshFilter;
		MeshRenderer meshRenderer;
		
		
		if (w % settings.chunkSize > 0)	chunkCountX = w / settings.chunkSize + 1; 
		else chunkCountX = w / settings.chunkSize; 
		if (h % settings.chunkSize > 0) chunkCountY = h / settings.chunkSize + 1; 
		else chunkCountY = h / settings.chunkSize;
		
		settings.chunkCountX = chunkCountX;
		settings.chunkCountY = chunkCountY;
		
		settings.chunks = new PATileTerrainChunk[chunkCountX * chunkCountY];
		settings.tiles = new PATile[w * h];
		settings.points = new PAPoint[(w + 1) * (h + 1)];
		settings.tsTrans.Clear();
		settings.tsTypes.Clear();	
		  
		for (cy = 0; cy < chunkCountY; ++cy)	
			for (cx = 0; cx < chunkCountX; ++cx) 
			{
				cId = chunkCountX * cy + cx;
				str = settings.name + "_chunk_" + cx + "x" + cy + "_" + cId;
				go = new GameObject(str);
				go.transform.parent = transform;
				go.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
				//go.isStatic = true;
			 
				chunk = go.AddComponent<PATileTerrainChunk>();
				settings.chunks[cId] = chunk;
				chunk.hideFlags = /*HideFlags.HideInInspector | */HideFlags.NotEditable;
				chunk.settings.x = cx;
				chunk.settings.y = cy;
				chunk.settings.id = cId;
				
				//MeshFilter
				meshFilter = go.AddComponent<MeshFilter>();
				meshFilter.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			
				//Mesh
				mesh = new Mesh();
				mesh.name = str;
				mesh.Clear();
			
				//MeshCollider
				meshCollider = go.AddComponent<MeshCollider>();
				meshCollider.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
				
				//MeshRenderer
				meshRenderer = go.AddComponent<MeshRenderer>();
				meshRenderer.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			
				//Creation of the mesh 
			
				//Chunk size
				if (cx == chunkCountX - 1) cw = settings.xCount - (chunkCountX - 1) * settings.chunkSize;	
				else cw = settings.chunkSize;
				if (cy == chunkCountY - 1) ch = settings.yCount - (chunkCountY - 1) * settings.chunkSize;	
				else ch = settings.chunkSize;
			
				go.transform.position = new Vector3(cx * settings.chunkSize * settings.tileSize, 0.0f, cy * settings.chunkSize * settings.tileSize) + tpos;
				
				verts = new List<Vector3>();
				uvs = new List<Vector2>();
				colors = new List<Color>();
				tris = new List<int>();
				normals = new List<Vector3>();
				
				for (y = 0; y < ch; ++y)	
					for (x = 0; x < cw; ++x) 
					{
						//Tiles
						sx = cx * settings.chunkSize + x;
						sy = cy * settings.chunkSize + y;
						
						i = w * sy + sx;
						li = cw * y + x;
					
						settings.tiles[i] = new PATile();
						tile = settings.tiles[i];
						tile.name = i.ToString();
						tile.id = i;
						tile.chunkId = cId;
						tile.x = sx;
						tile.y = sy;
						tile.cx = x;
						tile.cy = y;
						tile.cId = cw * y + x;
					
						tile.type = 0;
						tile.bits = 0;	
						
						//Center of tile
						tile.position = new Vector3(cx * settings.chunkSize * settings.tileSize, 0.0f, cy * settings.chunkSize * settings.tileSize) + 
										new Vector3(x * tileSize + tileSize / 2, 0.0f, y * tileSize + tileSize / 2);
					
						/*
						//vertices
						tile.verts[0] = new Vector3(x * tileSize, 0.0f, y * tileSize);
						tile.verts[1] = new Vector3(x * tileSize, 0.0f, y * tileSize + tileSize);
						tile.verts[2] = new Vector3(x * tileSize + tileSize, 0.0f, y * tileSize + tileSize);
						tile.verts[3] = new Vector3(x * tileSize + tileSize, 0.0f, y * tileSize);
						//uvs
						tile.uvs[0] = new Vector2(0.0f, 1.0f);
						tile.uvs[1] = new Vector2(1.0f, 1.0f);
						tile.uvs[2] = new Vector2(1.0f, 0.0f);
						tile.uvs[3] = new Vector2(0.0f, 0.0f);
						//tris			
						tile.tris[0] = li * 4 + 0;
						tile.tris[1] = li * 4 + 1;
						tile.tris[2] = li * 4 + 2;
						tile.tris[3] = li * 4 + 0;
						tile.tris[4] = li * 4 + 2;
						tile.tris[5] = li * 4 + 3;
						//normals 
						tile.normals[0] = new Vector3(0.0f, 1.0f, 0.0f);
						tile.normals[1] = new Vector3(0.0f, 1.0f, 0.0f);
						tile.normals[2] = new Vector3(0.0f, 1.0f, 0.0f);
						tile.normals[3] = new Vector3(0.0f, 1.0f, 0.0f);
						//colors
						tile.colors[0] = Color.white;
						tile.colors[1] = Color.white;
						tile.colors[2] = Color.white;
						tile.colors[3] = Color.white;
						
						for (j = 0; j < 6; ++j) 
						{
							if (j < 4)
							{
								verts.Add(tile.verts[j]);
								uvs.Add(tile.uvs[j]);
								colors.Add(tile.colors[j]);
								normals.Add(tile.normals[j]); 
							}
							tris.Add(tile.tris[j]);
						}
						*/
				
						//vertices
						verts.Add(new Vector3(x * tileSize, 0.0f, y * tileSize));
						verts.Add(new Vector3(x * tileSize, 0.0f, y * tileSize + tileSize));
						verts.Add(new Vector3(x * tileSize + tileSize, 0.0f, y * tileSize + tileSize));
						verts.Add(new Vector3(x * tileSize + tileSize, 0.0f, y * tileSize));
						//uvs
						uvs.Add(new Vector2(0.0f, 1.0f));
						uvs.Add(new Vector2(1.0f, 1.0f));
						uvs.Add(new Vector2(1.0f, 0.0f));
						uvs.Add(new Vector2(0.0f, 0.0f));
						//tris			
						tris.Add(li * 4 + 0);
						tris.Add(li * 4 + 1);
						tris.Add(li * 4 + 2);
						tris.Add(li * 4 + 0);
						tris.Add(li * 4 + 2);
						tris.Add(li * 4 + 3);
						//normals 
						normals.Add(new Vector3(0.0f, 1.0f, 0.0f));
						normals.Add(new Vector3(0.0f, 1.0f, 0.0f));
						normals.Add(new Vector3(0.0f, 1.0f, 0.0f));
						normals.Add(new Vector3(0.0f, 1.0f, 0.0f));
						//colors
						colors.Add(Color.white);
						colors.Add(Color.white);
						colors.Add(Color.white);
						colors.Add(Color.white);
													
					}				
								
				mesh.vertices = verts.ToArray();
				mesh.uv = uvs.ToArray();
				mesh.triangles = tris.ToArray();
				mesh.colors = colors.ToArray();	
				mesh.normals = normals.ToArray();
			
				meshFilter.sharedMesh = mesh;
				meshCollider.sharedMesh = mesh;
				meshRenderer.enabled = true;
				meshRenderer.castShadows = true;
				meshRenderer.receiveShadows = true;
				meshRenderer.sharedMaterial = settings.tilesetMaterial;
			
				chunk.settings.mesh = meshCollider.sharedMesh;
			}
		
		for (y = 0; y <= h; ++y)	
			for (x = 0; x <= w; ++x) 
			{
				//Prepare helper points for editor			
				i = (w + 1) * y + x;
				settings.points[i] = new PAPoint();
				point = settings.points[i];
				PrepareHelperPoint(point, x, y, w, h);		
			}
		
		SetTileSet(editorSettings.tileSetMaterial);
		
		settings.created = true;	
		settings.finalized = false;
		//gameObject.isStatic = true; //Not for InGame use
		
		UpdateMesh();
	}
	
	public void DestroyTerrain()
	{
		if (settings.chunks != null)
		foreach (PATileTerrainChunk c in settings.chunks) GameObject.DestroyImmediate(c.gameObject);
		settings.chunks = null;
		
		//GameObject.DestroyImmediate(gameObject.GetComponent<MeshCollider>());
		//GameObject.DestroyImmediate(gameObject.GetComponent<MeshFilter>());
		//GameObject.DestroyImmediate(gameObject.GetComponent<MeshRenderer>());
		settings.created = false;
		settings.finalized = false;
		
		settings.tiles = null;
		settings.points = null;
		settings.tsTrans.Clear();
		settings.tsTypes.Clear();
	}	
	
	public void FinalizeTerrain()	
	{
		settings.finalized = true;
		settings.points = null;	
	}
	
	public bool GetWalkability(int x, int y) { return settings.tiles[settings.xCount * y + x].walkability; }
	public bool GetWalkability(PATile tile) { return tile.walkability; }
	
	public bool LoadHeightMap(Texture2D tex, float min, float max)
	{
		if (tex == null) return false;
		
		float tw = tex.width, 
			  th = tex.height;
		float two = tw / settings.xCount, 
		      tho = th / settings.yCount; 
		float d, ch, h;
		int i, j, ii, jj, pi, mi;
		
		List<Mesh> ms = new List<Mesh>();
		List<Vector3[]> vs = new List<Vector3[]>();
		Mesh mesh;
		Vector3[] vertices;
		bool c;
		PATile tile;
		PAPoint point;

		Color clr = Color.black;
		
		if (min < max) d = max - min; else d = min - max;
		
		for (j = 0; j <= settings.yCount; ++j)	
			for (i = 0; i <= settings.xCount; ++i) 
			{
				if (i == settings.xCount) ii = i - 1; else ii = i;
				if (j == settings.yCount) jj = j - 1; else jj = j;
				
				clr = tex.GetPixelBilinear((ii * two) / tw, (jj * tho) / th);
				ch = (clr.r + clr.g + clr.b) / 3.0f;
				
				h = min + d * ch;
			
				//SetPointHeight(i, j, h, false);
				point = settings.points[(settings.xCount + 1) * j + i];
				for (pi = 0; pi < 4; ++pi)
				if (point.t[pi] >= 0)
				{
					c = false;
					tile = GetTile(point.t[pi]);
					mesh = GetChunkMesh(tile.chunkId);
					vertices = null;
					for (mi = 0; mi < ms.Count; ++mi) if (ms[mi] == mesh) { c = true; vertices = vs[mi]; }
					if (vertices == null) vertices = mesh.vertices;
				
					vertices[tile.cId * 4 + point.p[pi]].y = Mathf.Clamp(h, settings.minHeight, settings.maxHeight);
					
					if (!c) { ms.Add(mesh); vs.Add(vertices); }
				}	
				
			}
		for (i = 0; i < ms.Count; ++i) ms[i].vertices = vs[i];
			
		UpdateMesh();
		return true;
	}
	
	public bool FindPath(int startX, int startY, int targetX, int targetY, out PathData path)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		int onOpenList = 0, 
		    parentXval = 0, 
			parentYval = 0,
			a = 0, b = 0, m = 0, u = 0, v = 0, 
		    temp = 0, numberOfOpenListItems = 0,
			addedGCost = 0, tempGcost = 0,
			tempx, pathX, pathY,
			newOpenListItemID = 0,
			mapWidth = settings.xCount,
			mapHeight = settings.yCount;
		bool corner = false;
		bool found = false;
		int[] openList = new int[settings.xCount * settings.yCount + 2];
		int[,] whichList = new int[settings.xCount + 1, settings.yCount + 1];													
		int[] openX = new int[settings.xCount * settings.yCount + 2];
		int[] openY = new int[settings.xCount * settings.yCount + 2];
		int[,] parentX = new int[settings.xCount + 1, settings.yCount + 1];
		int[,] parentY = new int[settings.xCount + 1, settings.yCount + 1];
		int[] Fcost = new int[settings.xCount * settings.yCount + 2];
		int[,] Gcost = new int[settings.xCount + 1, settings.yCount + 1];
		int[] Hcost = new int[settings.xCount * settings.yCount + 2];
		int pathLength = 0;
		int pathLocation = 0;
		int onClosedList = 10;
		
		path = new PathData();
		path.data = null;
		path.length = 0;
		path.found = false;
		
		if (startX >= mapWidth || startX < 0) return false;
		else if (startY >= mapHeight || startY < 0) return false;
		else if (targetX >= mapWidth || targetX < 0) return false;
		else if (targetY >= mapHeight || targetY < 0) return false;
		
		if (startX == targetX && startY == targetY && pathLocation > 0) return false;
		if (startX == targetX && startY == targetY && pathLocation == 0) return false;
	
		if (!GetWalkability(targetX, targetY)) return false;
	
		if (onClosedList > 1000000) 
		{
			for (int x = 0; x < mapWidth; x++) 
				for (int y = 0; y < mapHeight; y++)	whichList[x, y] = 0;
			onClosedList = 10;	
		}
		onClosedList = onClosedList + 2;
		onOpenList = onClosedList - 1;
		pathLength = 0;
		pathLocation = 0;
		Gcost[startX, startY] = 0; 
	
		numberOfOpenListItems = 1;
		openList[1] = 1;
		openX[1] = startX ; openY[1] = startY;
	
		do
		{
			if (numberOfOpenListItems != 0)
			{
				parentXval = openX[openList[1]];
				parentYval = openY[openList[1]]; 
				whichList[parentXval, parentYval] = onClosedList;
				
				numberOfOpenListItems = numberOfOpenListItems - 1;
						
				openList[1] = openList[numberOfOpenListItems + 1];
				v = 1;
	
				do
				{
					u = v;		
					if (2 * u + 1 <= numberOfOpenListItems) 
					{
						if (Fcost[openList[u]] >= Fcost[openList[2 * u]]) v = 2 * u;
						if (Fcost[openList[v]] >= Fcost[openList[2 * u + 1]]) v = 2 * u + 1;		
					}
					else
					{
						if (2 * u <= numberOfOpenListItems) 	
							if (Fcost[openList[u]] >= Fcost[openList[2 * u]]) v = 2 * u;
					}
	
					if (u != v) 
					{
						temp = openList[u];
						openList[u] = openList[v];
						openList[v] = temp;			
					}
					else break; 
			
				} while (true);
	

				for (b = parentYval - 1; b <= parentYval + 1; b++)
				{
					for (a = parentXval - 1; a <= parentXval + 1; a++)
					{
				
						if (a != -1 && b != -1 && a != mapWidth && b != mapHeight)
						{
							
							if (whichList[a, b] != onClosedList) 
							{ 
							
								if (GetWalkability(a, b)) 
								{ 
								
									corner = true;	
									if (a == parentXval - 1) 
									{
										if (b == parentYval - 1)
										{
											if (!GetWalkability(parentXval - 1, parentYval) ||
												!GetWalkability(parentXval, parentYval - 1))
												corner = false;
										}
										else if (b == parentYval + 1)
										{
											if (!GetWalkability(parentXval, parentYval + 1) ||
												!GetWalkability(parentXval - 1, parentYval))												    
												corner = false; 
										}
									}
									else if (a == parentXval + 1)
									{
										if (b == parentYval - 1)
										{
											if (!GetWalkability(parentXval, parentYval - 1) ||
												!GetWalkability(parentXval + 1, parentYval))
												corner = false;
										}
										else if (b == parentYval + 1)
										{
											if (!GetWalkability(parentXval + 1, parentYval) ||
												!GetWalkability(parentXval, parentYval + 1))
												corner = false; 
										}
									}	
											
									if (corner == true) 
									{
								
										if (whichList[a,b] != onOpenList) 
										{	
											
											newOpenListItemID = newOpenListItemID + 1; 
											m = numberOfOpenListItems + 1;
											openList[m] = newOpenListItemID;
											openX[newOpenListItemID] = a;
											openY[newOpenListItemID] = b;
									
											if (Mathf.Abs(a - parentXval) == 1 && Mathf.Abs(b - parentYval) == 1) addedGCost = 14;
											else addedGCost = 10;			
											Gcost[a, b] = Gcost[parentXval, parentYval] + addedGCost;
									
											Hcost[openList[m]] = 10 * (Mathf.Abs(a - targetX) + Mathf.Abs(b - targetY));
											Fcost[openList[m]] = Gcost[a, b] + Hcost[openList[m]];
											parentX[a, b] = parentXval; 
											parentY[a, b] = parentYval;	
									
											while (m != 1) 
											{	
												if (Fcost[openList[m]] <= Fcost[openList[m / 2]])
												{
													temp = openList[m / 2];
													openList[m / 2] = openList[m];
													openList[m] = temp;
													m = m / 2;
												}	else break;
											}
											numberOfOpenListItems = numberOfOpenListItems + 1;
									
											whichList[a, b] = onOpenList;
										} else 
										{

											if (Mathf.Abs(a-parentXval) == 1 && Mathf.Abs(b - parentYval) == 1) addedGCost = 14;
											else addedGCost = 10;				
											tempGcost = Gcost[parentXval, parentYval] + addedGCost;
													
											if (tempGcost < Gcost[a, b]) 
											{
												parentX[a, b] = parentXval; 
												parentY[a, b] = parentYval;
												Gcost[a, b] = tempGcost;		
									
												for (int x = 1; x <= numberOfOpenListItems; x++) 
												{
													if (openX[openList[x]] == a && openY[openList[x]] == b) //item found
													{
														Fcost[openList[x]] = Gcost[a, b] + Hcost[openList[x]];
														m = x;
														while (m != 1) 
														{															
															if (Fcost[openList[m]] < Fcost[openList[m / 2]])
															{
																temp = openList[m / 2];
																openList[m / 2] = openList[m];
																openList[m] = temp;
																m = m / 2;
															}
															else
																break;
														} 
														break;
													} 
												} 
											}
										}
									}
								}
							}
						}
					}
				}
			
			} else { found = false; break; }  
		
			if (whichList[targetX, targetY] == onOpenList) { found = true; break; }
		
			}
			while (true);
		
			if (found)
			{
				path.found = true; 
				pathX = targetX; pathY = targetY;
				do
				{
					tempx = parentX[pathX, pathY];		
					pathY = parentY[pathX, pathY];
					pathX = tempx;
					pathLength = pathLength + 1;
				}
				while (pathX != startX || pathY != startY);
			
				path.length = pathLength + 1;
				path.data = new PATile[pathLength + 1];	
				path.data[0] = GetTile(startX, startY);
				pathX = targetX ; pathY = targetY;
				int ind = pathLength;

				do
				{
					path.data[ind] = GetTile(pathX, pathY);
					ind--;
				
					tempx = parentX[pathX,pathY];		
					pathY = parentY[pathX,pathY];
					pathX = tempx;
				}
				while (pathX != startX || pathY != startY);	
			}
			return found;	
	}
	
	public void SetTileSet(Material mat)
	{
		settings.tilesetMaterial = mat;
		foreach (PATileTerrainChunk c in settings.chunks)
		{
			c.renderer.sharedMaterial = mat;
		}
	}
	
	public PATileTerrain IsTerrain(Transform t)
	{ 
		foreach (PATileTerrainChunk c in settings.chunks) if (c.transform == t) return this;
		return null;
	}
	
	public void RecalcTilesetSizes()
	{
		settings.tilesetWidth = 1.0f / (float)settings.tilesetX;
		settings.tilesetHeight = 1.0f / (float)settings.tilesetY;
		settings.tilesetCount = settings.tilesetX * settings.tilesetY; 
	}
	
	public void AddNewTransition()
	{
		settings.tsTrans.Add(new PATSTransition());
	}
	
	protected PATSTransition FindTransition(int t1, int t2) 
	{ int id; return FindTransition(t1, t2, out id); }
	protected PATSTransition FindTransition(int t1, int t2, out int id)
	{
		int i;
		for (i = 0; i < settings.tsTrans.Count; ++i)
			if ((settings.tsTrans[i].from == t1 && settings.tsTrans[i].to == t2) ||
			    (settings.tsTrans[i].from == t2 && settings.tsTrans[i].to == t1)) 
		{ 
			id = i; 
			return settings.tsTrans[i]; 
		}
				
		id = -1;
		return null;
	}
	
	protected PATSTransition FindIntermediateTransition(int t1, int t2)	
	{ int id; return FindIntermediateTransition(t1, t2, out id); }
	protected PATSTransition FindIntermediateTransition(int t1, int t2, out int id)
	{
		int i, j, nt;
		for (i = 0; i < settings.tsTrans.Count; ++i)
		{
			if (settings.tsTrans[i].from == t1 || settings.tsTrans[i].to == t1) 
			{
				if (settings.tsTrans[i].from == t1) nt = settings.tsTrans[i].to;
				else nt = settings.tsTrans[i].from;
					
				for (j = 0; j < settings.tsTrans.Count; ++j)
					if (j != i)
					{
						if (settings.tsTrans[j].from == nt || settings.tsTrans[j].to == nt) 
						{							
							id = j;
							return settings.tsTrans[i];
						}
					}
			}
		}
		id = -1;
		return null;
	}
	
	protected PATSTransition FindTransitionForType(int t, out int id)
	{
		for (int i = 0; i < settings.tsTrans.Count; ++i)
			if (settings.tsTrans[i].from == t || settings.tsTrans[i].to == t) { id = i; return settings.tsTrans[i]; }
		id = -1;
		return null;
	}
	
	protected int FindTransitionId(int t)
	{
		for (int i = 0; i < settings.tsTrans.Count; ++i)
			if (settings.tsTrans[i].from == t || settings.tsTrans[i].to == t) return i;
		return -1;
	}
	
	protected int FindTransitionBitsId(byte bits)
	{
		for (int i = 0 ; i < 14; ++i) if (TRANSITION_BITS[i] == bits) return i;
		return -1;
	}
	
	public void AddNewType()
	{
		bool founded = false;
		int freeId = 0;
		do 
		{
			founded = false;
			foreach (PATSType t in settings.tsTypes)
			{
				if (freeId == t.id) { founded = true; break; }
			}
			
			if (!founded) break;
			freeId++;
		} while (true);
		
		
		PATSType nt = new PATSType();
		nt.id = freeId;
		nt.name = freeId.ToString();
		settings.tsTypes.Add(nt);
	}
	
	public void RemoveType(int index)
	{
		int tId = settings.tsTypes[index].id;
		foreach (PATSTransition t in settings.tsTrans)
		{
			if (t.from == tId) t.from = -1;
			if (t.to == tId) t.to = -1;
		}
		settings.tsTypes.RemoveAt(index);
	}
	
	public void RemoveTransition(int index)
	{
		settings.tsTrans.RemoveAt(index);
	}
	
	public void HideInUnity()
	{
		if (!settings.created) return;
		GameObject go;
		foreach (PATileTerrainChunk c in settings.chunks)
		{
			c.hideFlags = HideFlags.NotEditable;
			
			go = c.gameObject;
			go.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
			
			go.GetComponent<MeshFilter>().hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			go.GetComponent<MeshCollider>().hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			go.GetComponent<MeshRenderer>().hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
		}
	}
	
	protected void PrepareHelperPoint(PAPoint point, int x, int y, int w, int h)
	{
		for (int k = 0; k < 4; ++k) { point.t[k] = -1; point.p[k] = -1; }			
			
		if (x == 0) // only RU and RD
		{
			if (y == 0) //only RU
			{ 
				point.t[0] = 0; point.p[0] = 0; 
			} 
			else if (y == settings.yCount) //only RD
			{
				point.t[0] = w * (y - 1); point.p[0] = 1; 											
			} else //RU and RD
			{
				point.t[0] = w * y;       point.p[0] = 0; //RU
				point.t[1] = w * (y - 1); point.p[1] = 1; //RD
			}
		} else if (x == settings.xCount) //only LU and LD
		{
			if (y == 0) //only LU
			{ 
				point.t[0] = x - 1; point.p[0] = 3; 
			} 
			else if (y == settings.yCount) //only LD
			{ 
				point.t[0] = w * (y - 1) + x - 1; point.p[0] = 2; 
			} 
			else //LU and LD
			{ 
				point.t[0] = w * y + x - 1; 	  point.p[0] = 3; //LU
				point.t[1] = w * (y - 1) + x - 1; point.p[1] = 2; //LD
			}
		} else //if (x > 0 && x < settings.xCount)
		{
			if (y == 0) //only LU and RU
			{  
				point.t[0] = x - 1; point.p[0] = 3; //LU
				point.t[1] = x;     point.p[1] = 0; //RU
			} 
			else if (y == settings.yCount) //only LD and RD
			{ 
				point.t[0] = w * (y - 1) + x - 1; point.p[0] = 2; //LD  
				point.t[1] = w * (y - 1) + x; 	  point.p[1] = 1; //RD
			} 
			else //LU and RU and LD and RD
			{ 
				point.t[0] = w * y + x - 1; 	  point.p[0] = 3; //LU
				point.t[1] = w * y + x;           point.p[1] = 0; //RU
				point.t[2] = w * (y - 1) + x - 1; point.p[2] = 2; //LD
				point.t[3] = w * (y - 1) + x;     point.p[3] = 1; //RD
			}
		}
	}
	
	//Helpers
	protected static float LinearFalloff(float distance, float radius) 
	{ return Mathf.Clamp01(1.0f - distance / radius); }
	
	protected static float GaussFalloff(float distance, float radius)
	{ return Mathf.Clamp01 (Mathf.Pow (360.0f, -Mathf.Pow (distance / radius, 2.5f) - 0.01f)); }
	
	protected bool IsOnSameSide(Vector3 p1, Vector3 p2, Vector3 a, Vector3 b)
	{
		Vector3 bma = b - a;
		Vector3 cp1 = Vector3.Cross(bma, (p1 - a));
		Vector3 cp2 = Vector3.Cross(bma, (p2 - a));
		return (Vector3.Dot(cp1, cp2) >= 0.0f); 
	}
	protected static byte InvertBits(byte bits) { byte b = 240; b |= bits; return (byte)~b; }
	protected static bool IsBitSet(byte testBits, byte b) { return ((testBits & (byte)(1 << b)) == 1); }

	//Properties
	public int width { get { return settings.xCount; } }
	public int height { get { return settings.yCount; } }
	public float tileSize { get { return settings.tileSize; } }	
	public PATile[] tiles { get { return settings.tiles; } }
	public PAPoint[] points { get { return settings.points; } }
	public bool isCreated { get { return settings.created; } }
	
}
