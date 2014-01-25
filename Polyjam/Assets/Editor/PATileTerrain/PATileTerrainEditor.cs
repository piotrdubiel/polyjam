/* ===========================================================
 *  PATileTerrainEditor.cs
 *  Copyright (C) 2011-2012, Pozdnyakov Anton. 
 * v1.04
 * =========================================================== */
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[CustomEditor (typeof (PATileTerrainEdit))]
public class PATileTerrainEditor : Editor 
{
	public class Point { int x, y; }
		
	public PATileTerrain tileTerrain;
	public PATileTerrain.PATile selectedTile;
	private int menuToolbar = 0;
	private int brushSizePaint = 1;
	private int brushType = 0;
	private float brushSize = 2.0f;
	private Color brushColor = Color.white;
	private float brushPower = 0.1f;
	private bool brushSmooth = false;
	private bool brushRect = false;
	private List<PATileTerrain.PAPointXY> changedPoints = new List<PATileTerrain.PAPointXY>();
	private string[] brushNamesPaint = {"1x1", "3x3"};
	private int[] brushSizesPaint = {1, 3};
	private Texture2D heightMapFile = null;
	private float heightMapMin = 0.0f;
	private float heightMapMax = 10.0f;
	
	private GUIContent[] menu = new GUIContent[4];
	private Texture menuSettings;
	private Texture menuHeights;
	private Texture menuPaint;
	private Texture menuTile;
	private Texture2D[] menuBrushes;
	private int menuBrush = 0;
	private bool initialized = false;
	//private bool mouseDown = false;
	private Vector3 mousePos = Vector3.zero;
	private Material tileSetMaterial;
	private bool showTilesetInfo = true;
	private bool showTilesetType = true;
	private bool showTilesetTransitions = true;	
	private Vector2 tilesetScrollView = new Vector2();
	private Texture2D[] tilesetBrushesA = null;
	private GUIContent[] tilesetBrushesMenuA = null;
	private int tilesetBrushe = 0;
	private Texture2D whiteTex;
	private Texture2D blackTex;
	private Texture2D[] tilesetTransitionsA = null;
	
	//private string tilesetPath = "PATileTerrain/Sets/tileset";
		
	private Texture2D[] transitionsBits = new Texture2D[14];
	
	private const int MENU_SETTINGS = 0;
	private const int MENU_TILE = 1;
	private const int MENU_HEIGHTS = 2;
	private const int MENU_PAINT = 3;
	  
	private Projector previewProjector;
	private bool startAction = false;
	
	public void OnSceneGUI() 
    {
		if (tileTerrain == null || !tileTerrain.isCreated) return;
		
		RaycastHit hit;
		Ray ray;
		Rect winRect;
		int controlID = GUIUtility.GetControlID(FocusType.Passive); 
		Event current = Event.current;
		EditorWindow currentWindow = EditorWindow.mouseOverWindow;
		if (currentWindow)
		{
			winRect = currentWindow.position;
			
			if (menuToolbar == MENU_HEIGHTS)
			{
				ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
				if(Physics.Raycast(ray, out hit, Mathf.Infinity))
				{
					if (previewProjector != null)
						previewProjector.transform.position = hit.point;						
				}
			} else if (menuToolbar == MENU_PAINT || menuToolbar == MENU_TILE)
			{
				PATileTerrain.PATile tile = selectedTile;
				if (tile == null) tile = GetTile();
				
				if (tile != null && previewProjector != null)
				{
					/*1.04*/ //previewProjector.transform.position = tileTerrain.transform.position + tile.position;
					previewProjector.transform.position = tileTerrain.transform.TransformPoint(tile.position);
					previewProjector.transform.rotation = tileTerrain.transform.localRotation;
					previewProjector.transform.Rotate(90.0f, 0.0f, 0.0f);
				}
			}
			
			
			switch (current.type)
			{
			case EventType.MouseDown: 
				if (current.button == 0)
				{
					startAction = true;
					if (menuToolbar == MENU_HEIGHTS)
					{	
						SetHeights((current.shift == true));
					} else if (menuToolbar == MENU_PAINT)
					{
						Paint();
					}
				} else startAction = false;
				break;
			case EventType.MouseDrag:
				if (startAction && menuToolbar == MENU_HEIGHTS)
				{
					SetHeights((current.shift == true));
				} else if (startAction && menuToolbar == MENU_PAINT)
				{
					Paint();
				}
				
				HandleUtility.Repaint();
				mousePos.x = current.mousePosition.x - winRect.x;
				mousePos.y = winRect.height - current.mousePosition.y;
				break;
			case EventType.MouseUp: 
				startAction = false; 
				if (menuToolbar == MENU_TILE && current.button == 0)
				{
					selectedTile = GetTile();
					Repaint();
				} else if (menuToolbar == MENU_HEIGHTS && current.button == 0)
				{
					FinishSetHeights();
				} else if (menuToolbar == MENU_PAINT && current.button == 0)
				{
					FinishPaint();
				}
				break;
			case EventType.mouseMove:
				HandleUtility.Repaint();
				break;
			case EventType.repaint:
				mousePos.x = current.mousePosition.x - winRect.x;
				mousePos.y = winRect.height - current.mousePosition.y;
				break;
			case EventType.layout:
				HandleUtility.AddDefaultControl(controlID);
				break; 
			}
				
			
		}
    }
	
	public void OnEnable()
	{
		tileTerrain = GetSelectedTerrain();
		
		if (tileTerrain.settings.chunks != null)
		foreach (PATileTerrainChunk c in tileTerrain.settings.chunks)
		{
			EditorUtility.SetSelectedWireframeHidden(c.gameObject.renderer, true); 
		}
		tileTerrain.HideInUnity();
		
	}
	
	public void OnDisable()
	{
		menuToolbar = 0;
		selectedTile = null;
		tileTerrain = null;
	}
	
	public void Reset()
	{
		startAction = false;
		selectedTile = null;
		if (previewProjector != null) previewProjector.enabled = (menuToolbar == MENU_HEIGHTS || menuToolbar == MENU_PAINT || menuToolbar == MENU_TILE);
		UpdatePreview();
	}
	
	public void OnInit()
	{
		menuSettings = Resources.LoadAssetAtPath("Assets/PATileTerrain/Textures/settings.png", typeof(Texture)) as Texture;
		menuHeights = Resources.LoadAssetAtPath("Assets/PATileTerrain/Textures/heights_colors.png", typeof(Texture)) as Texture;
		menuPaint = Resources.LoadAssetAtPath("Assets/PATileTerrain/Textures/paint.png", typeof(Texture)) as Texture;
		menuTile = Resources.LoadAssetAtPath("Assets/PATileTerrain/Textures/tile.png", typeof(Texture)) as Texture;
			
		menu[MENU_SETTINGS] = new GUIContent(menuSettings, "Setting properties of the terrain");
		menu[MENU_TILE] = new GUIContent(menuTile, "Editing the properties of tile");
		menu[MENU_HEIGHTS] = new GUIContent(menuHeights, "Editing the height map");
		menu[MENU_PAINT] = new GUIContent(menuPaint, "Painting");
		
		whiteTex = Resources.LoadAssetAtPath("Assets/PATileTerrain/Textures/white.png", typeof(Texture2D)) as Texture2D;
		blackTex = Resources.LoadAssetAtPath("Assets/PATileTerrain/Textures/black.png", typeof(Texture2D)) as Texture2D;
			
		for (int i = 0; i < 14; ++i)	
			transitionsBits[i] = Resources.LoadAssetAtPath("Assets/PATileTerrain/Textures/bits_" + i + ".png", typeof(Texture2D)) as Texture2D;
		
		Texture2D brush;
		List<Texture2D> bList = new List<Texture2D>();
		int brushCount = 0;
		do 
		{
			brush = Resources.LoadAssetAtPath("Assets/PATileTerrain/Brushes/patileterrain_brush" + brushCount + ".png", typeof(Texture2D)) as Texture2D;
			if (brush != null) bList.Add(brush);
			brushCount++;
		} while (brush != null);
		menuBrushes = bList.ToArray();
		menuBrush = 0;
		tilesetBrushe = 0;
		
		InitPreview();
		UpdateTilesetBrushes();
		UpdateTilesetTransitions();
		
		initialized = true;
	}
	public void OnUninit()
	{
		UninitPreview();
		initialized = false;
	}
	
	public void SetTileSet(Material mat)
	{
		tileTerrain.SetTileSet(mat);
	}
	
	
	public void SetHeights(bool d)
	{		
		Event current = Event.current;
		RaycastHit hit;
		Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
		//Transform t = null;
		Vector3 pos;
		int x, y;
		float p;
		
		if (Physics.Raycast(ray, out hit, Mathf.Infinity)) 
		{
			PATileTerrain tt = tileTerrain.IsTerrain(hit.transform);
			if (tt != null) 
			{				
				/*[1.04]*/ //pos = hit.point - tileTerrain.transform.position;
				pos = tileTerrain.transform.InverseTransformPoint(hit.point);
				pos += new Vector3(tileTerrain.tileSize / 2, 0.0f, tileTerrain.tileSize / 2);
				x = (int)Mathf.Abs(pos.x / tileTerrain.tileSize);
				y = (int)Mathf.Abs(pos.z / tileTerrain.tileSize);
				
				if (brushType == 0)
				{
					if (brushSmooth)
					{
						tileTerrain.SmoothPointTerrain(x, y, brushRect, brushPower / 20.0f, brushSize, changedPoints);
					} else
					{
						if (d) p = -brushPower; else p = brushPower;			
						tileTerrain.DeformPointTerrain(x, y, brushRect, p, brushSize, changedPoints);
					}
				} else if (brushType == 1)
				{
					tileTerrain.SetVertexColors(x, y, brushRect, brushPower / 10.0f, brushColor, brushSize, changedPoints);
				}
			}
		}
	}
	
	public void FinishSetHeights()
	{ 
		tileTerrain.UpdateMesh(changedPoints);
		changedPoints.Clear();
	}
	
	public void Paint()
	{		
		if (brushSizePaint == 3) tileTerrain.PaintTile3x3(GetTile(), tilesetBrushe);
		else tileTerrain.PaintTile1x1(GetTile(), tilesetBrushe);	 	
	}
	
	public void FinishPaint()
	{
	}
	
	public PATileTerrain.PATile GetTile()
	{
		Event current = Event.current;
		RaycastHit hit;
		Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
		//Transform t = null;
		Vector3 pos;
		int x, y;
		
		if (Physics.Raycast(ray, out hit, Mathf.Infinity))
		{
			PATileTerrain tt = tileTerrain.IsTerrain(hit.transform);
			if (tt != null) 
			{
				/*[1.04]*/ //pos = hit.point - tileTerrain.transform.position;
				pos = tileTerrain.transform.InverseTransformPoint(hit.point);
				x = (int)Mathf.Abs(pos.x / tileTerrain.tileSize);
				y = (int)Mathf.Abs(pos.z / tileTerrain.tileSize);
				//Debug.Log("x = " + x + ", y = " + y);
				return tileTerrain.GetTile(x, y);
			}
				
		}
		return null;
	}
	
	public void UpdateTileset()
	{
		
	}
	
	public void UpdateTilesetTransitions()
	{
		int count = tileTerrain.settings.tsTrans.Count;
		if (count > 0)
		{
			tilesetTransitionsA = new Texture2D[count * 14];
			
			for (int t = 0; t < count; ++t) UpdateTilesetTransition(t);
		} else
		{
			tilesetTransitionsA = null;
		}
	}
	public void UpdateTilesetTransition(int t)
	{
		if (tilesetTransitionsA == null) return;
		
		int ind, id;
		int ix, iy;
		float x, y, cx, cy,
		      w = tileTerrain.settings.tilesetWidth, 
		      h = tileTerrain.settings.tilesetHeight;
		Texture2D mainTex = null;
		
		if (tileTerrain.settings.tilesetMaterial != null) mainTex = tileTerrain.settings.tilesetMaterial.mainTexture as Texture2D;	
		
		cx = w / 32.0f;
		cy = h / 32.0f;
		
		for (int k = 0; k < 14; ++k)
		{
			id = t * 14 + k;
			ind = tileTerrain.settings.tsTrans[t].transitions[k];
			iy = ind / tileTerrain.settings.tilesetX;
			ix = ind - iy * tileTerrain.settings.tilesetX;
			x = w * ix; 
			y = h * iy;
			
			tilesetTransitionsA[id] = new Texture2D(32, 32);
			try
			{
				if (mainTex != null)
				{
					for (int j = 0; j < 32; ++j)
					for (int i = 0; i < 32; ++i) 
					{
						tilesetTransitionsA[id].SetPixel(i, 32 - j, 
						                                 mainTex.GetPixelBilinear(x + i * cx, 1.0f - y - j * cy));
					}				
				}
			} catch {}
			tilesetTransitionsA[id].Apply();
		}
	}
	
	public void UpdateTilesetBrushes()
	{
		tilesetBrushe = 0;
		int count = tileTerrain.settings.tsTypes.Count;
		if (count > 0)
		{				
			tilesetBrushesA = new Texture2D[count];
			tilesetBrushesMenuA = new GUIContent[count];
			
			for (int t = 0; t < count; ++t) UpdateTilesetBrushe(t);
						
		} else
		{
			tilesetBrushesA = null;
			tilesetBrushesMenuA = null;
		}
	}
	public void UpdateTilesetBrushe(int t)
	{
		if (tilesetBrushesA == null) return;
		
		Color clr;
		int ind;
		int ix, iy;
		float x, y, cx, cy,
		      w = tileTerrain.settings.tilesetWidth, 
		      h = tileTerrain.settings.tilesetHeight;
		
		Texture2D mainTex = null;
		if (tileTerrain.settings.tilesetMaterial != null) mainTex = tileTerrain.settings.tilesetMaterial.mainTexture as Texture2D;	
		
		ind = tileTerrain.settings.tsTypes[t].baseIndexes[0];
		iy = ind / tileTerrain.settings.tilesetX;
		ix = ind - iy * tileTerrain.settings.tilesetX;
		x = w * ix; 
		y = h * iy;
		cx = w / 32.0f;
		cy = h / 32.0f;
		
		tilesetBrushesA[t] = new Texture2D(32, 32);
		try
		{
			if (mainTex != null)
			{
				for (int j = 0; j < 32; ++j)
					for (int i = 0; i < 32; ++i) 
					{
						clr = mainTex.GetPixelBilinear(x + i * cx, 1.0f - y - j * cy);
						tilesetBrushesA[t].SetPixel(i, 32 - j, clr);
					} 
			}
		} catch {}
		
		tilesetBrushesA[t].Apply();
		tilesetBrushesMenuA[t] = new GUIContent(tilesetBrushesA[t], tileTerrain.settings.tsTypes[t].name);
		
	}
	
	public void InitPreview()
	{ 
		UninitPreview();
		
		brushSmooth = false;
	    brushRect = false;
		menuBrush = 0;
		
		GameObject previewObject = new GameObject("PATileTerrainPreview");
		previewObject.hideFlags = HideFlags.HideInHierarchy;
		previewProjector = previewObject.AddComponent<Projector>();
		previewProjector.nearClipPlane = tileTerrain.editorSettings.minHeight - 1.0f;
        previewProjector.farClipPlane = tileTerrain.editorSettings.maxHeight + 1.0f;
        previewProjector.orthographic = true;
        previewProjector.orthographicSize = brushSize;
        previewProjector.transform.Rotate(-90.0f, 0.0f, 0.0f);
		 
		Shader previewShader = Shader.Find("Hidden/PATileTerrainPreview");
  		Material previewMaterial = new Material(previewShader);
		previewMaterial.SetColor("_Color", new Color(0.32f, 0.36f, 0.6f, 0.07f));
		previewMaterial.SetTexture("_Brush", menuBrushes[menuBrush]);
		previewProjector.material = previewMaterial;
		
		previewProjector.enabled = false;
		
		/*[1.04]*/ previewProjector.transform.parent = tileTerrain.transform; 
	}
	
	public void UpdatePreview()
	{
		if (previewProjector)
		{ 
			if (brushRect) menuBrush = 1;
			else menuBrush = 0;
			
			if (menuToolbar == MENU_PAINT || menuToolbar == MENU_TILE)
			{
				previewProjector.nearClipPlane = tileTerrain.settings.minHeight - 1.0f;
        		previewProjector.farClipPlane = tileTerrain.settings.maxHeight + 1.0f;
				previewProjector.material.SetColor("_Color", new Color(0.32f, 0.36f, 0.6f, 0.97f));
				previewProjector.material.SetTexture("_Brush", menuBrushes[1]);
				
				if (menuToolbar == MENU_PAINT)
					previewProjector.orthographicSize = (tileTerrain.settings.tileSize * 0.6f)* brushSizePaint;
				else previewProjector.orthographicSize = tileTerrain.settings.tileSize * 0.6f;
			} else
			{
				previewProjector.nearClipPlane = tileTerrain.settings.minHeight - 1.0f;
        		previewProjector.farClipPlane = tileTerrain.settings.maxHeight + 1.0f;
				previewProjector.material.SetColor("_Color", new Color(0.32f, 0.36f, 0.6f, 0.97f));
				previewProjector.material.SetTexture("_Brush", menuBrushes[menuBrush]);
				previewProjector.orthographicSize = brushSize;
			} 
		}
	}
	
	public void UninitPreview()
	{
		Projector[] projectors = GameObject.FindObjectsOfType(typeof(Projector)) as Projector[];
		foreach (Projector p in projectors)
		{
			if (p.gameObject.name == "PATileTerrainPreview")
					GameObject.DestroyImmediate(p.gameObject);
		}
		
	}
	 
	public override void OnInspectorGUI () 
    {		
		if (tileTerrain == null) return;
                
        EditorGUIUtility.LookLikeInspector();
		
		EditorGUILayout.BeginVertical();         
       
		if (!tileTerrain.isCreated)
		{	
			GUILayout.Label("Terrain Settings", EditorStyles.boldLabel);
			GUILayout.BeginVertical("Box");
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
			tileTerrain.editorSettings.name = EditorGUILayout.TextField(tileTerrain.editorSettings.name);
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("X : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
			tileTerrain.editorSettings.x = EditorGUILayout.IntSlider(tileTerrain.editorSettings.x, 1, 1024);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Y : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
			tileTerrain.editorSettings.y = EditorGUILayout.IntSlider(tileTerrain.editorSettings.y, 1, 1024);
			GUILayout.EndHorizontal();	
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Chunk size : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
			int m = Mathf.Min(tileTerrain.editorSettings.x, tileTerrain.editorSettings.y);
			m = Mathf.Min(16, m);
			tileTerrain.editorSettings.chunkSize = EditorGUILayout.IntSlider(tileTerrain.editorSettings.chunkSize, 1, m);
			GUILayout.EndHorizontal();	
				
			GUILayout.BeginHorizontal();
			GUILayout.Label("Tile Size : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
			tileTerrain.editorSettings.tileSize = EditorGUILayout.Slider(tileTerrain.editorSettings.tileSize, 0.01f, 32.0f);
			GUILayout.EndHorizontal();	
			GUILayout.BeginHorizontal();
			GUILayout.Label("Min Height : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
			tileTerrain.editorSettings.minHeight = EditorGUILayout.Slider(tileTerrain.editorSettings.minHeight, -256.0f, 256.0f);
			GUILayout.EndHorizontal();	
			GUILayout.BeginHorizontal();
			GUILayout.Label("Max Height : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
			tileTerrain.editorSettings.maxHeight = EditorGUILayout.Slider(tileTerrain.editorSettings.maxHeight, -256.0f, 256.0f);
			GUILayout.EndHorizontal();	
			GUILayout.BeginHorizontal();
			GUILayout.Label("Material : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
			tileTerrain.editorSettings.tileSetMaterial = EditorGUILayout.ObjectField(tileTerrain.editorSettings.tileSetMaterial, typeof(Material), (tileTerrain.editorSettings.tileSetMaterial != null)?EditorUtility.IsPersistent(tileTerrain.editorSettings.tileSetMaterial):false) as Material;
			GUILayout.EndHorizontal(); 
			GUILayout.EndVertical(); 
			
			if (GUILayout.Button("Create Terrain")) 
        	{
				EditorUtility.DisplayProgressBar("Creating...", "Please wait...", 1.0f);
				
            	tileTerrain.CreateTerrain();
				 
				//save meshes
				string assetPath;
				string assetFolderGUID = AssetDatabase.CreateFolder("Assets", "PATileTerrain/" + tileTerrain.settings.name);
				string assetFolder = AssetDatabase.GUIDToAssetPath(assetFolderGUID);
				AssetDatabase.StartAssetEditing();
				
				foreach (PATileTerrainChunk c in tileTerrain.settings.chunks)
				{
					assetPath = assetFolder + "/" + c.name + ".asset";					
					AssetDatabase.CreateAsset(c.settings.mesh, assetPath);
					EditorUtility.SetSelectedWireframeHidden(c.gameObject.renderer, true); 
				}
				AssetDatabase.StopAssetEditing();
				
				EditorUtility.ClearProgressBar();
				OnInit();
        	}   
			
		} else
		{		
			if (!initialized) OnInit();
			//EditorUtility.SetSelectedWireframeHidden(tileTerrain.renderer, !tileTerrain.editorSettings.debugWireframe); 
			
			int tbar = menuToolbar;
			bool b, showTrans;
			float f;
			int i, j, n;
			Texture2D tex;
			Material mat;
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Tile Terrain Editor", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			tbar = GUILayout.Toolbar(menuToolbar, menu, GUILayout.Width(256), GUILayout.Height(30), GUILayout.MaxWidth(256));
			if (tbar != menuToolbar)
			{
				menuToolbar = tbar;
				Reset();
			}	
			
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			
			switch (menuToolbar)
			{
			case MENU_SETTINGS:
				GUILayout.Label("Terrain info (read only)", EditorStyles.boldLabel);
					GUILayout.BeginVertical("Box");
					GUILayout.BeginHorizontal();
					GUILayout.Label("Width  : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					GUILayout.Label(tileTerrain.settings.xCount.ToString());
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Label("Height : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					GUILayout.Label(tileTerrain.settings.yCount.ToString());
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Label("Chunk size : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					GUILayout.Label(tileTerrain.settings.chunkSize.ToString());
					GUILayout.EndHorizontal();	
					GUILayout.BeginHorizontal();
					GUILayout.Label("Tile Size : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					GUILayout.Label(tileTerrain.settings.tileSize.ToString());
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Label("Min Height : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					GUILayout.Label(tileTerrain.settings.minHeight.ToString());
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Label("Max Height : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					GUILayout.Label(tileTerrain.settings.maxHeight.ToString());
					GUILayout.EndHorizontal();
					/*GUILayout.BeginHorizontal();
					GUILayout.Label("Mesh : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					EditorGUILayout.ObjectField(tileTerrain.terrainMesh, typeof(Mesh));
					GUILayout.EndHorizontal();*/
					GUILayout.EndVertical();		
				  
					/*GUILayout.Label("Debug");
					GUILayout.BeginVertical("Box");
					tileTerrain.editorSettings.debugWireframe = GUILayout.Toggle(tileTerrain.editorSettings.debugWireframe, "Draw Wireframe");
					//tileGUILayoutTerrain.editorSettings.debugNormals = GUILayout.Toggle(tileTerrain.editorSettings.debugNormals, "Draw Normals");
					GUILayout.EndVertical();*/
				
				if (GUILayout.Button("Update Terrain")) 
        		{
					UpdateMesh();
        		}
				
				break;
			case MENU_TILE:
					if (selectedTile == null)
					{
						GUILayout.Label("Select a tile on the terrain!", EditorStyles.boldLabel);
					} else
					{
						GUILayout.Label("Tile", EditorStyles.boldLabel);
						GUILayout.BeginVertical("Box");
						GUILayout.BeginHorizontal();
						GUILayout.Label("X : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						GUILayout.Label(selectedTile.x.ToString() + " (read only)");
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label("Y : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						GUILayout.Label(selectedTile.y.ToString() + " (read only)");
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
						GUILayout.Label("Id : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						selectedTile.id = EditorGUILayout.IntField(selectedTile.id);
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label("Name : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						selectedTile.name = EditorGUILayout.TextField(selectedTile.name);
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label("Walkability : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						selectedTile.walkability = EditorGUILayout.Toggle(selectedTile.walkability);
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label("Custom Data : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						selectedTile.customData = EditorGUILayout.ObjectField(selectedTile.customData, typeof(Object), (selectedTile.customData != null)?EditorUtility.IsPersistent(selectedTile.customData):false);
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
						
					}
					break;
			case MENU_HEIGHTS:	
				if (!tileTerrain.settings.finalized) 
				{
					string[] ss = {"Edit Heights", "Edit Vertex Colors"};
					int[] ids = {0, 1};
					brushType = EditorGUILayout.IntPopup(brushType, ss, ids, GUILayout.MaxWidth(128));
					
					EditorGUILayout.Space();
					
					GUILayout.Label("Brush", EditorStyles.boldLabel);
					GUILayout.BeginVertical("Box");						
					
					/*GUILayout.Label("Brushes : ", EditorStyles.boldLabel);
					GUILayout.BeginHorizontal("box");
					i = GUILayout.SelectionGrid(menuBrush, menuBrushes, 8, "gridlist", GUILayout.Width(256), GUILayout.Height(24));
					if (i != menuBrush)
					{
						menuBrush = i;
						UpdatePreview();
					}
					GUILayout.EndHorizontal();*/
					
					GUILayout.BeginHorizontal();
					GUILayout.Label("Size : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					f = EditorGUILayout.Slider(brushSize, 0.1f, 10.0f);
					if (f != brushSize)
					{
						brushSize = f;
						UpdatePreview();
					}
					GUILayout.EndHorizontal();		
					GUILayout.BeginHorizontal();
					GUILayout.Label("Power : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
					brushPower = EditorGUILayout.Slider(brushPower, 0.01f, 1.0f);
					GUILayout.EndHorizontal();	
					
					if (brushType == 1)
					{
						GUILayout.BeginHorizontal();
						brushColor = EditorGUILayout.ColorField(brushColor);
						GUILayout.EndHorizontal();	
					}
					
					GUILayout.BeginHorizontal();
					b = GUILayout.Toggle(brushRect, "Rectangle", GUILayout.MaxWidth(96));
					if (b != brushRect)
					{
						brushRect = b;
						UpdatePreview();
					}
					GUILayout.EndHorizontal();	
					if (brushType == 0)
					{
						GUILayout.BeginHorizontal();
						brushSmooth = GUILayout.Toggle(brushSmooth, "Smooth", GUILayout.MaxWidth(96));
						GUILayout.EndHorizontal();	
					}
					GUILayout.EndVertical();
					
					if (brushType == 0)
					{
						bool show = true;
						GUILayout.Label("Height Map", EditorStyles.boldLabel);
						GUILayout.BeginVertical("Box");						
					
						GUILayout.BeginHorizontal();
						GUILayout.Label("Height Map File: ", EditorStyles.boldLabel);
						GUILayout.EndHorizontal();	
						GUILayout.BeginHorizontal();
						tex = EditorGUILayout.ObjectField(heightMapFile, typeof(Texture2D), (heightMapFile != null)?EditorUtility.IsPersistent(heightMapFile):false) as Texture2D;
						if (tex != heightMapFile)
						{
							heightMapFile = tex;
							try
							{
								heightMapFile.GetPixel(0, 0);
							} catch 
							{
								GUILayout.Label("Texture must be readable!");
								GUILayout.Label("Texture does not match the requirements!");	
								show = false;
							}
						}
						GUILayout.EndHorizontal();	
						
						GUILayout.BeginHorizontal();
						GUILayout.Label("Min Height: ", EditorStyles.boldLabel);
						heightMapMin = EditorGUILayout.Slider(heightMapMin, tileTerrain.settings.minHeight, tileTerrain.settings.maxHeight);
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label("Max Height: ", EditorStyles.boldLabel);
						heightMapMax = EditorGUILayout.Slider(heightMapMax, tileTerrain.settings.minHeight, tileTerrain.settings.maxHeight);
						GUILayout.EndHorizontal();
						
						if (show)
						if (GUILayout.Button("Load height map")) 
						if (EditorUtility.DisplayDialog  ("Confirm Load" , "Do you really want to load height map?", "Yes", "No"))
						{
							EditorUtility.DisplayProgressBar("Loading height map...", "Please wait...", 1.0f);
							tileTerrain.LoadHeightMap(heightMapFile, heightMapMin, heightMapMax);
							EditorUtility.ClearProgressBar();
						}
						
						GUILayout.EndVertical();
					}
				}
				break;
			case MENU_PAINT:				
				if (!tileTerrain.settings.finalized) 
				{
					if (tilesetBrushesMenuA != null)
					{
						GUILayout.BeginVertical("Box");		
						GUILayout.Label("Brush", EditorStyles.boldLabel);
						GUILayout.BeginHorizontal();
						i = GUILayout.SelectionGrid(tilesetBrushe, tilesetBrushesMenuA, 8, "gridlist", GUILayout.Width(256), GUILayout.Height(24));
						if (i != tilesetBrushe)
						{
							tilesetBrushe = i;
							UpdateTileset();
						}
						GUILayout.EndHorizontal();
						
						GUILayout.BeginHorizontal();
						i = EditorGUILayout.IntPopup("Size : ", brushSizePaint, brushNamesPaint, brushSizesPaint);
						if (i != brushSizePaint)
						{
							brushSizePaint = i;
							UpdatePreview();
						}
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
					}
						
					showTrans = true;
					try
					{
						(tileTerrain.settings.tilesetMaterial.mainTexture as Texture2D).GetPixel(0, 0);
					} catch
					{
						showTrans = false;
					}
					
					//GUILayout.BeginVertical("Box");
					showTilesetInfo = EditorGUILayout.Foldout(showTilesetInfo, "Tile Set");
					if (showTilesetInfo)
					{
						//int tileSetX, tileSetY;
						
						//if (tilesetBrushesMenuA != null)
						//	GUILayout.Toolbar(1, tilesetBrushesMenuA, GUILayout.Width(tilesetBrushesMenuA.Length * 32), GUILayout.Height(32), GUILayout.MaxWidth(256));
						
						GUILayout.Label("New tileset size", EditorStyles.boldLabel);
						GUILayout.BeginVertical("Box");
						GUILayout.BeginHorizontal();
						GUILayout.Label("X : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						tileTerrain.editorSettings.tilesetX = EditorGUILayout.IntSlider(tileTerrain.editorSettings.tilesetX, 1, 128);
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label("Y : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						tileTerrain.editorSettings.tilesetY = EditorGUILayout.IntSlider(tileTerrain.editorSettings.tilesetY, 1, 128);
						GUILayout.EndHorizontal();
						
						if (GUILayout.Button("Set new tileset size")) 
						if (EditorUtility.DisplayDialog  ("Confirm Change" , "Do you really want to change tileset size?", "Yes", "No"))
						{
							tileTerrain.settings.tilesetX = tileTerrain.editorSettings.tilesetX;
							tileTerrain.settings.tilesetY = tileTerrain.editorSettings.tilesetY;
							tileTerrain.RecalcTilesetSizes();
							UpdateTilesetBrushes();
						}
						
						GUILayout.EndVertical();
						
						GUILayout.Label("Current tileset size", EditorStyles.boldLabel);
						GUILayout.BeginVertical("Box");
						GUILayout.BeginHorizontal();
						GUILayout.Label("X : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						GUILayout.Label(tileTerrain.settings.tilesetX.ToString());
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label("Y : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						GUILayout.Label(tileTerrain.settings.tilesetY.ToString());
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label("Count : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						GUILayout.Label(tileTerrain.settings.tilesetCount.ToString());
						GUILayout.EndHorizontal();
						
						GUILayout.BeginHorizontal();
						GUILayout.Label("OffsetX : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						tileTerrain.settings.uvOffset.x = EditorGUILayout.FloatField(tileTerrain.settings.uvOffset.x);
						GUILayout.EndHorizontal();
						
						GUILayout.BeginHorizontal();
						GUILayout.Label("OffsetY : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						tileTerrain.settings.uvOffset.y = EditorGUILayout.FloatField(tileTerrain.settings.uvOffset.y);
						GUILayout.EndHorizontal();
						
						GUILayout.BeginHorizontal();
						GUILayout.Label("Material : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						mat = EditorGUILayout.ObjectField(tileTerrain.settings.tilesetMaterial, typeof(Material), (tileTerrain.settings.tilesetMaterial != null)?EditorUtility.IsPersistent(tileTerrain.settings.tilesetMaterial):false) as Material;
						if (mat != tileTerrain.settings.tilesetMaterial)
						{
							SetTileSet(mat);
							
							UpdateTilesetBrushes();
							UpdateTilesetTransitions();
							Repaint();
							
						} 
						GUILayout.EndHorizontal();	
						
						tilesetScrollView = EditorGUILayout.BeginScrollView(tilesetScrollView, GUILayout.Height(150), GUILayout.MaxHeight(150));
						if (tileTerrain.settings.tilesetMaterial != null) 
							GUILayout.Label(tileTerrain.settings.tilesetMaterial.mainTexture, GUILayout.MaxHeight(128), GUILayout.MaxWidth(256));
						//GUILayout.Box(tileTerrain.settings.tilesetMaterial.mainTexture, GUILayout.Width(256), GUILayout.MaxHeight(128));
						EditorGUILayout.EndScrollView();
						
						if (!showTrans) 
						{
							GUILayout.Label("Texture must be readable!");
							GUILayout.Label("Texture does not match the requirements!");
						} 
						
						GUILayout.EndVertical();
					}
					//GUILayout.EndVertical();	
					
					showTilesetType = EditorGUILayout.Foldout(showTilesetType, "Tile Type"); 
					if (showTilesetType)
					{
						GUILayout.BeginVertical("Box");
						if (tileTerrain.settings.tsTypes.Count > 0)
						{	
							int rem = -1, rem2 = -1;
							int ind;
							string tn;
							for (i = 0; i < tileTerrain.settings.tsTypes.Count; ++i)
							{
								PATileTerrain.PATSType type = tileTerrain.settings.tsTypes[i];
								if (type.name.Length <= 0) tn = "Type - " + (i + 0);
								else tn = "Type - " + type.name; 
								
								type.show = EditorGUILayout.Foldout(type.show, tn); 
								if (type.show)
								{
									GUILayout.BeginVertical("Box");
									
									GUILayout.BeginHorizontal();
									GUILayout.Label("Id : ", EditorStyles.boldLabel, GUILayout.MaxWidth(128));
									GUILayout.Label(type.id.ToString());
									GUILayout.EndHorizontal();
									
									GUILayout.BeginHorizontal();
									GUILayout.Label("Name : ", EditorStyles.boldLabel, GUILayout.MaxWidth(128));
									type.name = EditorGUILayout.TextField(type.name);
									GUILayout.EndHorizontal();
									
									GUILayout.BeginHorizontal();
									GUILayout.Label("Base indexes : ", EditorStyles.boldLabel, GUILayout.MaxWidth(128));
									//GUILayout.Label(tilesetBrushesA[i], GUILayout.Width(32), GUILayout.Height(32));
									GUILayout.EndHorizontal();
									
									GUILayout.BeginVertical("Box");
									for (j = 0; j < type.baseIndexes.Count; ++j)
									{
									 	GUILayout.BeginHorizontal();
										GUILayout.Label("Index " + j + ": ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
										ind = EditorGUILayout.IntSlider(type.baseIndexes[j], 0, tileTerrain.settings.tilesetCount);
										if (ind != type.baseIndexes[j])
										{
											type.baseIndexes[j] = ind;
											UpdateTilesetBrushe(i);
										}
										if (j == 0)
											GUILayout.Label(tilesetBrushesA[i], GUILayout.Width(32), GUILayout.Height(32));
										
										GUILayout.EndHorizontal();
						
										EditorGUILayout.Space();
										if (GUILayout.Button("Remove base index " + j)) rem2 = j;	
									}
									if (rem2 != -1)
									{
										if (type.baseIndexes.Count > 1) 
										{
											type.RemoveBaseIndex(rem2);
											Repaint();
										}
									}								
									GUILayout.EndVertical();
								
									EditorGUILayout.Space();
									if (GUILayout.Button("Fill terrain")) 
									if (EditorUtility.DisplayDialog  ("Confirm Fill" , "Do you really want to fill the terrain with this type?", "Yes", "No"))
									{
										tileTerrain.FillTerrain(i);
									}
										
									if (GUILayout.Button("Add base index")) 
									{
										type.AddBaseIndex();
										Repaint();
									}
									if (GUILayout.Button("Remove type")) rem = i;										
									
									GUILayout.EndVertical();
								}
							}
							
							if (rem != -1)
							{
								tileTerrain.RemoveType(rem);
								UpdateTilesetBrushes();
								Repaint();
							}
							
						}
						
						EditorGUILayout.Space();
						if (GUILayout.Button("Add Type")) 
						{
							tileTerrain.AddNewType();
							UpdateTilesetBrushes();
							Repaint();
						}
						GUILayout.EndVertical();
					}
					
					//GUILayout.BeginVertical("Box");
					if (showTrans)
					{
						showTilesetTransitions = EditorGUILayout.Foldout(showTilesetTransitions, "Tile Transition"); 
						if (showTilesetTransitions)
						{
							GUILayout.BeginVertical("Box");
							if (tileTerrain.settings.tsTrans.Count > 0)
							{	
								int rem = -1, ind;
								string tn;
								List<PATileTerrain.PATSType> types = tileTerrain.settings.tsTypes;
								for (i = 0; i < tileTerrain.settings.tsTrans.Count; ++i)
								{
									PATileTerrain.PATSTransition transition = tileTerrain.settings.tsTrans[i];
									
									if (transition.name.Length <= 0) tn = "Transition - " + (i + 0);
									else tn = "Transition - " + transition.name; 
									
									transition.show = EditorGUILayout.Foldout(transition.show, tn); 
									if (transition.show)
									{
										string[] tps = new string[types.Count + 1];
										int[] ids = new int[types.Count + 1];
										ids[0] = -1;
										tps[0] = "none";
										for (j = 1; j <= types.Count; ++j)
										{
											if (types[j - 1].name.Length <= 0) tps[j - 1] = (j - 1).ToString();
											else tps[j] = types[(j - 1)].name;
											 
											ids[j] = j - 1;
										}
										
										
										GUILayout.BeginVertical("Box");
										
										GUILayout.BeginHorizontal();
										GUILayout.Label("From : ", EditorStyles.boldLabel, GUILayout.MaxWidth(128));
										n = EditorGUILayout.IntPopup(transition.from, tps, ids, GUILayout.MaxWidth(128)); 
										if (n == transition.to) transition.from = -1;
										else transition.from = n;
										 //EditorGUILayout.IntField(transition.type);
										GUILayout.EndHorizontal();
										
										GUILayout.BeginHorizontal();
										GUILayout.Label("To : ", EditorStyles.boldLabel, GUILayout.MaxWidth(128));
										n = EditorGUILayout.IntPopup(transition.to, tps, ids, GUILayout.MaxWidth(128)); 
										if (n == transition.from) transition.to = -1;
										else transition.to = n;
										//transition.toType = EditorGUILayout.IntField(transition.toType);
										GUILayout.EndHorizontal();
										
										GUILayout.BeginHorizontal();
										GUILayout.Label("Name : ", EditorStyles.boldLabel, GUILayout.MaxWidth(128));
										transition.name = EditorGUILayout.TextField(transition.name);
										GUILayout.EndHorizontal();
										
										GUILayout.BeginHorizontal();
										GUILayout.Label("Transitions : ", EditorStyles.boldLabel, GUILayout.MaxWidth(128));
										GUILayout.EndHorizontal();
										
										GUILayout.BeginHorizontal();
										GUILayout.Label("From - ", GUILayout.MaxWidth(40));
										GUILayout.Label(blackTex, GUILayout.Width(16), GUILayout.Height(16));
										GUILayout.Label("To - ", GUILayout.MaxWidth(24));
										GUILayout.Label(whiteTex, GUILayout.Width(16), GUILayout.Height(16));
										GUILayout.EndHorizontal();
										
										for (j = 0; j < 14; ++j)	
										{
											//transitionsBits[i]
											GUILayout.BeginHorizontal();
											GUILayout.Label(transitionsBits[j], GUILayout.Width(32), GUILayout.Height(32));
											//GUILayout.Label("Index : ", EditorStyles.boldLabel, GUILayout.MaxWidth(96));
											ind = EditorGUILayout.IntSlider(transition.transitions[j], 0, tileTerrain.settings.tilesetCount - 1);
											if (ind != transition.transitions[j])
											{
												transition.transitions[j] = ind;
												UpdateTilesetTransition(i);
											}
											GUILayout.Label(tilesetTransitionsA[i * 14 + j], GUILayout.Width(32), GUILayout.Height(32));
											
											GUILayout.EndHorizontal();
										}
										
										EditorGUILayout.Space();
										if (GUILayout.Button("Remove transition")) rem = i;										
										
										GUILayout.EndVertical();
									}
								}
								if (rem != -1)
								{
									tileTerrain.RemoveTransition(rem);
									UpdateTilesetTransitions();
									Repaint();
								}
								
							}
							
							EditorGUILayout.Space();
							if (GUILayout.Button("Add Transition")) 
							{
								tileTerrain.AddNewTransition();
								UpdateTilesetTransitions();
								Repaint();
							}
							GUILayout.EndVertical();
						}
					}
					//GUILayout.EndVertical();
				}
				break;
			}
			
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			
			GUILayout.BeginVertical("Box");
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Save tileset"))
			{
				string path = EditorUtility.SaveFilePanel("Save tileset", "Assets", "tileset", "txt");
				if (path.Length != 0)
				{
					SaveTileset(path);
					AssetDatabase.Refresh();
				}
			}
			if (GUILayout.Button("Open tileset"))
			{
				string path = EditorUtility.OpenFilePanel("Open tileset", "Assets", "txt");
				if (path.Length != 0)
				{
					OpenTileset(path);
					AssetDatabase.Refresh();
					
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			
			EditorGUILayout.Space();
			
			GUILayout.BeginVertical("Box");
			GUILayout.BeginHorizontal();
			if (!tileTerrain.settings.finalized)
			{
				/*if (GUILayout.Button("Finalize Terrain")) 
				if (EditorUtility.DisplayDialog  ("Confirm Finalize" , "Do you really want to finalize the terrain? \nAfter finalization the editing of heights and tiling will be impossible!", "Yes", "No"))
				{
					tileTerrain.Finalize();
				}*/
			}
			if (GUILayout.Button("Destroy Terrain")) 
			if (EditorUtility.DisplayDialog  ("Confirm Destroy" , "Do you really want to destroy the terrain?", "Yes", "No"))
			{
				AssetDatabase.StartAssetEditing();
				foreach (PATileTerrainChunk c in tileTerrain.settings.chunks)
				{
					AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(c.settings.mesh));
				}
				AssetDatabase.StopAssetEditing();
				
            	tileTerrain.DestroyTerrain();
				
				OnUninit();
				Repaint();
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
		
        EditorGUILayout.EndVertical();  
        EditorGUILayout.Separator(); 
				
        
    }
	
	public void SaveTileset(string path)
	{
		File.Delete(path);
		StreamWriter f = File.CreateText(path);
		int i, j;
		
		f.WriteLine("1"); //Version
		f.WriteLine(AssetDatabase.GetAssetPath(tileTerrain.settings.tilesetMaterial)); 
		f.WriteLine(tileTerrain.settings.tilesetX);
		f.WriteLine(tileTerrain.settings.tilesetY);
		f.WriteLine(tileTerrain.settings.uvOffset.x);
		f.WriteLine(tileTerrain.settings.uvOffset.y);
		
		//Types
		f.WriteLine(tileTerrain.settings.tsTypes.Count);
		for (i = 0; i < tileTerrain.settings.tsTypes.Count; ++i)
		{
			f.WriteLine(tileTerrain.settings.tsTypes[i].id);
			f.WriteLine(tileTerrain.settings.tsTypes[i].name);			
			f.WriteLine(tileTerrain.settings.tsTypes[i].baseIndexes.Count);			
			for (j = 0; j < tileTerrain.settings.tsTypes[i].baseIndexes.Count; ++j)	
				f.WriteLine(tileTerrain.settings.tsTypes[i].baseIndexes[j]);
		}
		
		//Transitions
		f.WriteLine(tileTerrain.settings.tsTrans.Count);
		for (i = 0; i < tileTerrain.settings.tsTrans.Count; ++i)
		{
			f.WriteLine(tileTerrain.settings.tsTrans[i].from);
			f.WriteLine(tileTerrain.settings.tsTrans[i].to);
			f.WriteLine(tileTerrain.settings.tsTrans[i].name);			
			for (j = 0; j < 14; ++j) 
				f.WriteLine(tileTerrain.settings.tsTrans[i].transitions[j]);			
		}
		
		f.Close();
		
	}
	
	public void UpdateMesh()
	{	
		EditorUtility.DisplayProgressBar("Updating...", "Please wait...", 1.0f);
		tileTerrain.UpdateMesh();
		EditorUtility.ClearProgressBar();
	}	
	
	public void OpenTileset(string path)
	{
		StreamReader f = File.OpenText(path);
		int i, j, c, c2;
		//string s;
		
		int version = int.Parse(f.ReadLine());
		if (version == 1)
		{
			tileTerrain.settings.tsTypes.Clear();
			tileTerrain.settings.tsTrans.Clear();
			
			tileTerrain.settings.tilesetMaterial = Resources.LoadAssetAtPath(f.ReadLine(), typeof(Material)) as Material;
			tileTerrain.settings.tilesetX = int.Parse(f.ReadLine());
			tileTerrain.settings.tilesetY = int.Parse(f.ReadLine());
			tileTerrain.settings.uvOffset.x = float.Parse(f.ReadLine());
			tileTerrain.settings.uvOffset.y = float.Parse(f.ReadLine());
			
			c = int.Parse(f.ReadLine());
			for (i = 0; i < c; ++i)
			{
				PATileTerrain.PATSType t = new PATileTerrain.PATSType();
				t.id = int.Parse(f.ReadLine());
				t.name = f.ReadLine();
	
				t.baseIndexes.Clear();
				c2 = int.Parse(f.ReadLine());
				for (j = 0; j < c2; ++j) 
				{
					t.AddBaseIndex(int.Parse(f.ReadLine()));
				}
				
				tileTerrain.settings.tsTypes.Add(t);
			}
			
			c = int.Parse(f.ReadLine());
			for (i = 0; i < c; ++i)
			{
				PATileTerrain.PATSTransition t = new PATileTerrain.PATSTransition();
				t.from = int.Parse(f.ReadLine());
				t.to = int.Parse(f.ReadLine());
				t.name = f.ReadLine();
				
				for (j = 0; j < 14; ++j)
				{
					t.transitions[j] = int.Parse(f.ReadLine());
				}
				tileTerrain.settings.tsTrans.Add(t);
			}
		}
		
		f.Close();		
	
		SetTileSet(tileTerrain.settings.tilesetMaterial);	
		tileTerrain.RecalcTilesetSizes();
		
		UpdateTilesetBrushes();
		UpdateTilesetTransitions();
		Repaint();
		
	}
	
	public PATileTerrain GetSelectedTerrain()
	{
		PATileTerrainEdit edit = target as PATileTerrainEdit;
		if (edit.tileTerrain == null) 
		{
			PATileTerrain t = edit.gameObject.AddComponent<PATileTerrain>();
			edit.tileTerrain = t;
		}
		edit.tileTerrain.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable | HideFlags.HideInHierarchy;
		
		return edit.tileTerrain;
	}
}
