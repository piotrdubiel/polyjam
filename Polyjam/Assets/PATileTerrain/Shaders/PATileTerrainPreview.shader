Shader "Hidden/PATileTerrainPreview" {
  Properties {
  	 _Color ("Main Color", Color) = (1,1,1,1)   	
     _Brush ("Brush", 2D) = "black" { TexGen ObjectLinear }
  }
  Subshader {
     Pass {
        ZWrite off
        Fog { Color (1, 1, 1) }
        Color [_Color]
        ColorMask RGB 
        Blend SrcColor OneMinusSrcColor
		Offset -1, -1
		
        SetTexture [_Brush] 
        {
        	combine texture * primary
        	Matrix [_Projector]
        }
       
     }
  }
}

