// Unlit drop-shadow/blur shader

Shader "f3/f3DropShadow" {
Properties {
//	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Color ("Color", Color) = (0,0,0,1)
	_AlphaScale ("AlphaScale", Float) = 1 
	_FalloffWidth ("FalloffWidth", Float) = 1.0
	_Center ("Center", Vector) = (0, 0, 0, 0)
	_Extents ("Extents", Vector) = (5, 5, 0,0)
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha 
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
//				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float3 vertexo : COLOR;
//				half2 texcoord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			};

//			sampler2D _MainTex;
//			float4 _MainTex_ST;
			fixed4 _Color;
			float _AlphaScale;
			float _FalloffWidth;
			float4 _Center;
			float4 _Extents;

			
			v2f vert (appdata_t v)
			{
				v2f o;

				// compute model-space coordinates
				float4 posM = mul(UNITY_MATRIX_M, v.vertex);
				o.vertexo = posM.xyz;

				o.vertex = UnityObjectToClipPos(v.vertex);
//				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{

				float3 pos = i.vertexo;

				float dx = abs( pos.x - _Center.x );
				float dy = abs( pos.y - _Center.y );
				float d = 0;
				float w = _Extents.x - _FalloffWidth;
				float h = _Extents.y - _FalloffWidth;
				if ( dx > w )
					d += (dx-w)*(dx-w);
				if ( dy > h )
					d += (dy-h)*(dy-h);
				float t = 0;
				d = d / (_FalloffWidth*_FalloffWidth);
				if (d < 1) {
					t = (1-d);
					t = t*t*t;
				}
				float4 c = _AlphaScale * float4(_Color.x,_Color.y,_Color.z,_Color.a*t);

				fixed4 col = c;
				col.a *= _AlphaScale;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
		ENDCG
	}
}

}
