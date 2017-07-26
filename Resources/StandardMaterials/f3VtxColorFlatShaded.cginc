// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

#ifndef f3VtxColorFlatShaded_INCLUDED
#define f3VtxColorFlatShaded_INCLUDED


// [RMS] below from UnityStandardInput.cginc


//
// [RMS] define new input vtx structure that has color value
// 
struct VertexInput_f3VC
{
	float4 vertex	: POSITION;
	fixed4 color    : COLOR;			// [RMS] added this
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
	float2 uv1		: TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	float2 uv2		: TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
	half4 tangent	: TANGENT;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


//
// [RMS] have to define new version of this function because we have a new structure name
//
float4 TexCoords_f3VC(VertexInput_f3VC v)
{
	float4 texcoord;
	texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
	texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
	return texcoord;
}	



// [RMS] below from UnityStandardCore.cginc





// [RMS] have to duplicate this function because of input struct name
inline half4 VertexGIForward_f3VC(VertexInput_f3VC v, float3 posWorld, half3 normalWorld)
{
    half4 ambientOrLightmapUV = 0;
    // Static lightmaps
    #ifdef LIGHTMAP_ON
        ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        ambientOrLightmapUV.zw = 0;
    // Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
    #elif UNITY_SHOULD_SAMPLE_SH
        #ifdef VERTEXLIGHT_ON
            // Approximated illumination from non-important point lights
            ambientOrLightmapUV.rgb = Shade4PointLights (
                unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                unity_4LightAtten0, posWorld, normalWorld);
        #endif

        ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, ambientOrLightmapUV.rgb);
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    return ambientOrLightmapUV;
}




// ------------------------------------------------------------------
//  Base forward pass (directional light, emission, lightmaps, ...)


//
// [RMS] added color field
//
struct VertexOutputForwardBase_f3VC
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndPackedData[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax or worldPos]
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UV
	UNITY_SHADOW_COORDS(6)
	UNITY_FOG_COORDS(7)

	fixed4 color                        : COLOR;		// [RMS] added this for vtx color
	float3 mynormal                     : NORMAL;

	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
	#if UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT
	float3 posWorld                 : TEXCOORD8;
	#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};



// [RMS] added one line for color
VertexOutputForwardBase_f3VC vertForwardBase_f3VC (VertexInput_f3VC v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputForwardBase_f3VC o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase_f3VC, o);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	#if UNITY_REQUIRE_FRAG_WORLDPOS
	#if UNITY_PACK_WORLDPOS_WITH_TANGENT
		o.tangentToWorldAndPackedData[0].w = posWorld.x;
		o.tangentToWorldAndPackedData[1].w = posWorld.y;
		o.tangentToWorldAndPackedData[2].w = posWorld.z;
	#else
		o.posWorld = posWorld.xyz;
	#endif
	#endif
	o.pos = UnityObjectToClipPos(v.vertex);
		
	o.tex = TexCoords_f3VC(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
	#ifdef _TANGENT_TO_WORLD
		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
		o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
		o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
		o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
	#else
		o.tangentToWorldAndPackedData[0].xyz = 0;
		o.tangentToWorldAndPackedData[1].xyz = 0;
		o.tangentToWorldAndPackedData[2].xyz = normalWorld;
	#endif
	o.mynormal = normalWorld;

	//We need this for shadow receving
	UNITY_TRANSFER_SHADOW(o, v.uv1);

	o.ambientOrLightmapUV = VertexGIForward_f3VC(v, posWorld, normalWorld);
	
	#ifdef _PARALLAXMAP
		TANGENT_SPACE_ROTATION;
		half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
		o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
		o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
		o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
	#endif

	// [RMS] this is the only line we added!
	o.color = v.color;

	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}


// [RMS] added multiply by color & alpha
half4 fragForwardBaseInternal_f3VC (VertexOutputForwardBase_f3VC i)
{
	FRAGMENT_SETUP(s);
	//s.normalWorld = i.mynormal;

	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	UnityLight mainLight = MainLight ();
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

	half occlusion = Occlusion(i.tex.xy);
	UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	c.rgb += Emission(i.tex.xy);

	c *= i.color;			// [RMS] multiply by our input color

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);
	//	return OutputForward (c, s.alpha);
	return OutputForward (c, s.alpha * i.color.a);		// [RMS] multiply by input alpha (necessary?)
}

half4 fragForwardBase_f3VC (VertexOutputForwardBase_f3VC i) : SV_Target	// backward compatibility (this used to be the fragment entry function)
{
	return fragForwardBaseInternal_f3VC(i);
}







// ------------------------------------------------------------------
//  Deferred pass

// [RMS] added color member
struct VertexOutputDeferred_f3VC
{
	float4 pos							: SV_POSITION;
	fixed4 color                        : COLOR;		// [RMS] added
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndPackedData[3]: TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax or worldPos]
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UVs

	#if UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT
		float3 posWorld                     : TEXCOORD6;
	#endif

	UNITY_VERTEX_OUTPUT_STEREO
};

// [RMS] added color line
VertexOutputDeferred_f3VC vertDeferred_f3VC (VertexInput_f3VC v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputDeferred_f3VC o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputDeferred_f3VC, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	#if UNITY_REQUIRE_FRAG_WORLDPOS
	#if UNITY_PACK_WORLDPOS_WITH_TANGENT
		o.tangentToWorldAndPackedData[0].w = posWorld.x;
		o.tangentToWorldAndPackedData[1].w = posWorld.y;
		o.tangentToWorldAndPackedData[2].w = posWorld.z;
	#else
		o.posWorld = posWorld.xyz;
	#endif
	#endif
	o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords_f3VC(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
	#ifdef _TANGENT_TO_WORLD
		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
		o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
		o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
		o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
	#else
		o.tangentToWorldAndPackedData[0].xyz = 0;
		o.tangentToWorldAndPackedData[1].xyz = 0;
		o.tangentToWorldAndPackedData[2].xyz = normalWorld;
	#endif

	o.ambientOrLightmapUV = 0;
	#ifdef LIGHTMAP_ON
		o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#elif UNITY_SHOULD_SAMPLE_SH
		o.ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, o.ambientOrLightmapUV.rgb);
	#endif
	#ifdef DYNAMICLIGHTMAP_ON
		o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
	
	#ifdef _PARALLAXMAP
		TANGENT_SPACE_ROTATION;
		half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
		o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
		o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
		o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
	#endif

	// [RMS] transfer color to output
	o.color = v.color;

	return o;
}


// [RMS] added lines to multiply color & alpha by input color i.color
void fragDeferred_f3VC (
	VertexOutputDeferred_f3VC i,
	out half4 outGBuffer0 : SV_Target0,
	out half4 outGBuffer1 : SV_Target1,
	out half4 outGBuffer2 : SV_Target2,
	out half4 outEmission : SV_Target3			// RT3: emission (rgb), --unused-- (a)
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
	,out half4 outShadowMask : SV_Target4       // RT4: shadowmask (rgba)
#endif
)
{
	#if (SHADER_TARGET < 30)
		outGBuffer0 = 1;
		outGBuffer1 = 1;
		outGBuffer2 = 0;
		outEmission = 0;
		#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
		outShadowMask = 1;
		#endif
		return;
	#endif

	FRAGMENT_SETUP(s)

	// no analytic lights in this pass
	UnityLight dummyLight = DummyLight ();
	half atten = 1;

	// only GI
	half occlusion = Occlusion(i.tex.xy);
#if UNITY_ENABLE_REFLECTION_BUFFERS
	bool sampleReflectionsInDeferred = false;
#else
	bool sampleReflectionsInDeferred = true;
#endif


	UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

	half3 emissiveColor = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
	emissiveColor *= i.color;		// [RMS] multiply by color

	#ifdef _EMISSION
		emissiveColor += Emission (i.tex.xy);
	#endif

	#ifndef UNITY_HDR_ON
		emissiveColor.rgb = exp2(-emissiveColor.rgb);
	#endif

	UnityStandardData data;
	data.diffuseColor	= s.diffColor * i.color.rgb;		// [RMS] multiply by our color
	data.occlusion		= occlusion;		
	data.specularColor	= s.specColor * i.color.rgb;		// [RMS] multiply by our color
	data.smoothness		= s.smoothness;	
	data.normalWorld	= s.normalWorld;

	UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

	// Emisive lighting buffer
	outEmission = half4(emissiveColor, 1);

	// Baked direct lighting occlusion if any
	#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
	outShadowMask = UnityGetRawBakedOcclusions(i.ambientOrLightmapUV.xy, IN_WORLDPOS(i));
	#endif
}








#endif