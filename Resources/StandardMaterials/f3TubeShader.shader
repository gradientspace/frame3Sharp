//
//  Tube shader. Assumes input is line geometry, emits N-sided tube around each line.
//  Input per-vertex normal and tangent.xyz define per-vertex frames.
//  Input tangent.w is used as a scaling factor on constant tube radius.
//  Input color is ignored unless _PerVertexColors=1
//
//  Currently user per-vertex normals and diffuse shading with main light.
//


Shader "f3/TubeShader" 
{
	Properties 
	{
		_Color("Color", Color) = (1,1,1,1)

		[Toggle]
		_PerVertexColors("PerVertexColors", Float) = 0.0

		[Toggle]
		_DiffuseLighting("Enable Lighting", Float) = 1.0

		_Emission("Emission Power", Float) = 0.5

		_Radius("Tube Radius", Float) = 0.5
		_TubeN("Tube Slices", Range(3,15)) = 6		// limited by maxvertexcount() in geometry shader

		// [RMS] this seems to have no significant effect...
		[HideInInspector]
		[Toggle(_PER_VERTEX_LIGHT_DIR)]
		_PerVertexLightDir("Per Vertex Light Dir", Float) = 0.0
	}
	SubShader 
	{
	
	    Tags { "RenderType" = "Opaque" }
		//Blend SrcAlpha OneMinusSrcAlpha

		Cull Off

    	Pass 
    	{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#pragma target 4.0

			#pragma shader_feature _PER_VERTEX_LIGHT_DIR

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			

			
			half4 _Color;
			uint _TubeN;
			float _Radius;
			float _PerVertexColors;
			float _DiffuseLighting;
			float _Emission;

			static const float PI = 3.14159265f;


			struct vert_in
			{
				float4  vertex : POSITION;
				float3  normal : NORMAL;
				float4  tangent : TANGENT;
				half4   color : COLOR;
				float2  uv : TEXCOORD0;
			};

			struct vert2geom 
			{
				float4  vertex : POSITION;
				float3  normal : NORMAL;
				float4  tangent : TANGENT;
				half4   color : COLOR;
    			float2  uv : TEXCOORD0;
			};
			
			struct geom2frag 
			{
    			float4  pos : POSITION;
				float3  normal : NORMAL;
				half4   color : COLOR;
				float2  uv : TEXCOORD0;
				float3  worldpos : TEXCOORD1;
				float3  lightDir  : TEXCOORD2;
			};

			vert2geom vert(vert_in v)
			{
				vert2geom vout;
				vout.vertex = v.vertex;
				vout.normal = v.normal;
				vout.tangent = v.tangent;
				vout.uv = v.uv;
				vout.color = v.color;
    			return vout;
			}
			
			// if tube slices is N, this value needs to be at least 2*N+2
			[maxvertexcount(32)]
			void geom(line vert2geom IN[2], inout TriangleStream<geom2frag> triStream)
			{
				// line endpoints
				float3 p0 = IN[0].vertex;
				float3 p1 = IN[1].vertex;

				// extract endpoint frames
				float3 n0 = IN[0].normal;
				float3 n1 = IN[1].normal;
				float3 t0 = IN[0].tangent.xyz;
				float3 t1 = IN[1].tangent.xyz;

				float r0 = _Radius * IN[0].tangent.w;
				float r1 = _Radius * IN[1].tangent.w;

				// start/end frames
				float3 dx0 = n0;
				float3 dy0 = t0;
				float3 dx1 = n1;
				float3 dy1 = t1;

				geom2frag vtx0;
				vtx0.normal = dx0;
				vtx0.worldpos = p0 + r0 * vtx0.normal;
				vtx0.pos = UnityObjectToClipPos(vtx0.worldpos);
				vtx0.lightDir = ObjSpaceLightDir(IN[0].vertex);
				vtx0.color = IN[0].color;
				vtx0.uv = IN[0].uv;
				triStream.Append(vtx0);

				geom2frag vtx1;
				vtx1.normal = dx1;
				vtx1.worldpos = p1 + r1 * vtx1.normal;
				vtx1.pos = UnityObjectToClipPos(vtx1.worldpos);
				vtx1.lightDir = ObjSpaceLightDir(IN[1].vertex);
				vtx1.color = IN[1].color;
				vtx1.uv = IN[1].uv;
				triStream.Append(vtx1);

				for (uint i = 1; i <= _TubeN; ++i) {
					float t = (float)i / (float)_TubeN;
					t *= 2.0 * PI;
					float c = cos(t), s = sin(t);

					vtx0.normal = c * dx0 + s * dy0;
					vtx0.worldpos = p0 + r0 * vtx0.normal;
					vtx0.pos = UnityObjectToClipPos(vtx0.worldpos);
					triStream.Append(vtx0);

					vtx1.normal = c * dx1 + s * dy1;
					vtx1.worldpos = p1 + r1 * vtx1.normal;
					vtx1.pos = UnityObjectToClipPos(vtx1.worldpos);
					triStream.Append(vtx1);
				}
			}

			
			half4 frag(geom2frag i) : COLOR
			{
				half4 color = lerp(_Color, i.color, _PerVertexColors);
				if (_DiffuseLighting == 0)
					return color;

				// this is useful...http://kylehalladay.com/blog/tutorial/bestof/2013/10/13/Multi-Light-Diffuse.html

				fixed atten = 1.0;

				#ifdef _PER_VERTEX_LIGHT_DIR
					i.lightDir = ObjSpaceLightDir(float4(i.worldpos, 1));
				#endif
				i.lightDir = normalize(i.lightDir);

				fixed diff = saturate(dot(i.normal, i.lightDir));
				fixed4 c;
				c.rgb = (UNITY_LIGHTMODEL_AMBIENT.rgb + _Emission) * color.rgb;
				c.rgb += (color.rgb * diff) * (atten);
				// [RMS] these are equations from post above. However they result in shading that
				//   is *much* too dim. Need to incorporate other lights, I think...
				//c.rgb += (color.rgb * _LightColor0.rgb * diff) * (atten);
				//c.a = color.a + _LightColor0.a * atten;
				c.a = 1.0;
				return c;
			}
			
			ENDCG

    	}
	}
}