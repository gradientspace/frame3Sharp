// [RMS] This is our super-shader. features:
//   * derived from standard unity shader w/ metallic BRDF (but no texture maps)
//   * vertex colors
//   * backface control - culling mode, two-sided shaded, backface visualization modes
//   * wireframe w/ color and width
//   * facegroup colors - triangle index is passed to fragment shader
//                      - fragment shader maps tid to groupid via index texture map
//                      - groupid is used to look up color in group color map
//
//   Most of these can be enabled/disabled in Unity editor.
//   Mainly controlled via conditional compilation, so no cost when disabled.
//   ** currently vertex color replaces input color
//   ** currently group color replaces input/vertex color
//
Shader "f3/StandardMeshShader"
{
	Properties
	{
		_Color("Color", Color) = (0.5,0.5,0.5,1)

		[Toggle(_ENABLE_VERTEX_COLOR)]
		_EnableVertexColor("Use Vertex Colors", Float) = 0.0

		[Header(Backface Settings)]

		// Backface Culling state, 0=Off (default), 1=Front, 2=Back
		_Cull("Backface Culling Mode", Int) = 0

		[KeywordEnum(None, Grid, Scallop, Wave)] _Backface("BackFace Shading Mode", Float) = 2

		_BackFaceFreq("Backface Frequency", Range(0.0,10.0)) = 3.0
		_BackFaceColor("Backface Color", Color) = (1.0,0.55,0.55,1)
		_BackFaceColor2("Backface Color 2", Color) = (1.0,0.45,0.15,1)

		[Header(Mesh Wireframe Settings)]

		[Toggle(_ENABLE_WIREFRAME)] 
		_Wireframe("Enable Wireframe", Float) = 0.0

		_WireColor("Wire Color", Color) = (0,0,0,1)
		_WireWidth("Wire Width", Range(0.0,10.0)) = 1.0


		[Header(Clip Plane Settings)]

		[KeywordEnum(Off, Clip, ClipFill)] _EnableClipPlane("Enable Clip Plane", Float) = 0
		_ClipPlaneColor("Clip Fill Color", Color) = (1,1,0,1)
		//_ClipPlaneEquation("ClipPlane", Vector) = (0,1,0,-9999)

		[Header(Material Properties)]

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

		// emission color is multiplied by its own alpha before adding to output color
		_EmissionColor("Emission Color", Color) = (0,0,0,0)
		_BackfaceEmissionColor("Backface Emission", Color) = (1,1,1,0.2)


		[Header(FaceGroup Color Settings)]

		[Toggle(_FACE_GROUP_COLORS)]
		_EnableFaceGroupColors("Enable Face Group Colors", Float) = 0

		[NoScaleOffset] _FaceIndexMap("Face Index Map", 2D) = "black" {}
		[NoScaleOffset] _GroupColorMap("Group Color Map", 2D) = "white" {}

        // Blending state
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0

	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT MetallicSetup
	ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" "PerformanceChecks" = "False" }
		LOD 300

		Cull [_Cull]


		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 4.0

			// -------------------------------------

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF

			#pragma shader_feature _ENABLE_VERTEX_COLOR
			#pragma shader_feature _FACE_GROUP_COLORS
			#pragma multi_compile _BACKFACE_NONE _BACKFACE_GRID _BACKFACE_SCALLOP _BACKFACE_WAVE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vertForwardBase_f3VC
			#pragma geometry geomForwardBase
			#pragma fragment fragForwardBase_f3VC
			#include "UnityStandardCoreForward.cginc"
			#include "f3StandardMeshShader.cginc"

            [maxvertexcount(3)]
            void geomForwardBase(triangle VertexOutputForwardBase_f3VC input[3], uint pid : SV_PrimitiveID, inout TriangleStream<VertexOutputForwardBase_f3VC> OutputStream)
            {
				// for flat-shading, we rewrite normal
				float3 p0 = IN_WORLDPOS(input[0]);
				float3 p1 = IN_WORLDPOS(input[1]);
				float3 p2 = IN_WORLDPOS(input[2]);
                float3 normal = normalize(cross(p1 - p0, p2 - p0));
				input[0].tangentToWorldAndPackedData[2].xyz = normal;
				input[1].tangentToWorldAndPackedData[2].xyz = normal;
				input[2].tangentToWorldAndPackedData[2].xyz = normal;

				// for wireframe, we construct edge distance functions
				// that are interpolated over triangles. We can't store
				// squared-distances here because of the linear interpolation...

				input[0].tri_id = pid;
				input[1].tri_id = pid;
				input[2].tri_id = pid;

				//frag position
				float2 WIN_SCALE = float2(_ScreenParams.x / 2.0, _ScreenParams.y / 2.0);
				float2 screen0 = WIN_SCALE * input[0].pos.xy / input[0].pos.w;
				float2 screen1 = WIN_SCALE * input[1].pos.xy / input[1].pos.w;
				float2 screen2 = WIN_SCALE * input[2].pos.xy / input[2].pos.w;

				//edge lengths and triangle area
				float2 e0 = screen2 - screen1;
				float2 e1 = screen2 - screen0;
				float2 e2 = screen1 - screen0;
				float area = abs(e1.x*e2.y - e1.y*e2.x);

				input[0].dist = float3(area / length(e0), 0, 0);
				input[1].dist = float3(0, area / length(e1), 0);
				input[2].dist = float3(0, 0, area / length(e2));

				OutputStream.Append(input[0]);
				OutputStream.Append(input[1]);
				OutputStream.Append(input[2]);
            }



			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 4.0

			// -------------------------------------


			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF

			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog

			// [RMS] also need to do geom shader here if we want specular highlights...

			#pragma vertex vertForwardAdd_f3VC
			#pragma geometry geomForwardAdd
			#pragma fragment fragForwardAdd_f3VC
			#include "UnityStandardCoreForward.cginc"
			#include "f3StandardMeshShader.cginc"

			[maxvertexcount(3)]
			void geomForwardAdd(triangle VertexOutputForwardAdd input[3], inout TriangleStream<VertexOutputForwardAdd> OutputStream)
			{
				float3 p0 = IN_WORLDPOS_FWDADD(input[0]);
				float3 p1 = IN_WORLDPOS_FWDADD(input[1]);
				float3 p2 = IN_WORLDPOS_FWDADD(input[2]);
				float3 normal = normalize(cross(p1 - p0, p2 - p0));
				input[0].tangentToWorldAndLightDir[2].xyz = normal;
				input[1].tangentToWorldAndLightDir[2].xyz = normal;
				input[2].tangentToWorldAndLightDir[2].xyz = normal;

				OutputStream.Append(input[0]);
				OutputStream.Append(input[1]);
				OutputStream.Append(input[2]);
			}


			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster_f3VC
			#pragma fragment fragShadowCaster_f3VC
			
			// [RMS] have to define this to get extra variables in shader??
            #ifdef SHADER_API_D3D11
			#define UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
            #endif

			#include "UnityStandardShadow.cginc"
			#include "f3StandardMeshShader_Shadow.cginc"

			ENDCG
		}


	}












	// crappier shader??

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 150

		Cull [_Cull]

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			// [RMS] can maybe now target SM 2.0 ??
			//#pragma target 2.0
			// [RMS] apparently have to use SM3.0 for vertex color shader, otherwise we get an
			//   error about too many inputs
			#pragma target 3.0

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION 
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF

			#pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vertForwardBase_f3VC
			#pragma fragment fragForwardBase_f3VC
			#include "UnityStandardCoreForward.cginc"
			#include "f3StandardMeshShader.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma skip_variants SHADOWS_SOFT
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#include "UnityStandardCoreForward.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}


	}


	FallBack "VertexLit"
	//CustomEditor "StandardShaderGUI"
	CustomEditor "PrependBlendModeShaderGUI"
}
