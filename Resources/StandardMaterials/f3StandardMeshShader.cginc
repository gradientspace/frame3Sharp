// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

#ifndef f3StandardMesh_INCLUDED
#define f3StandardMesh_INCLUDED


// variables from shader

float _BackFaceFreq;
half4 _BackFaceColor;
half4 _BackFaceColor2;
float _Wireframe;
half4 _WireColor;
float _WireWidth;
half4 _BackfaceEmissionColor;

float _EnableClipPlane;
half4 _ClipPlaneColor;
float4 _ClipPlaneEquation;

float4 _SceneOriginWorld;

// [RMS] use separate texture/state so that we can get "nearest" sampling
//  (alternately can configure this state in texture)
// https://docs.unity3d.com/Manual/SL-SamplerStates.html
// [RMS] actually now we use Texture2D.Load() instead of Sample(), so we 
//   don't need a sampler at all!

Texture2D _FaceIndexMap;
float4 _FaceIndexMap_TexelSize;		// built-in variable {1/width, 1/height, width, height}
Texture2D _GroupColorMap;
float4 _GroupColorMap_TexelSize;	// built-in variable {1/width, 1/height, width, height}

// [RMS] below from UnityStandardInput.cginc


//
// [RMS] define new input vtx structure that has color value
// 
struct VertexInput_f3VC
{
	float4 vertex	: POSITION;
	half4 color     : COLOR;			// [RMS] added this
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
	float2 uv1		: TEXCOORD1;
#ifdef _TANGENT_TO_WORLD
	half4 tangent	: TANGENT;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};



// [RMS] below from UnityStandardCore.cginc





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
	half4 tangentToWorldAndPackedData[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3 worldPos]
	UNITY_SHADOW_COORDS(6)
	UNITY_FOG_COORDS(7)

	half4 vtx_color                     : COLOR;		// [RMS] added this for vtx color
	float3 dist							: TEXCOORD5;	// for wireframe
	uint tri_id							: COLOR1;

	// [RMS] UNITY_PACK_WORLDPOS_WITH_TANGENT=0 on mobile devices 
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
		
	o.tex = 0;  // [RMS] no textures w/ this shader TexCoords_f3VC(v);
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

	//We need this for shadow receving
	UNITY_TRANSFER_SHADOW(o, v.uv1);

	// [RMS] this is the only line we added!
	o.vtx_color = v.color;

	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}






// [RMS] replaced Albedo() call with input color, got rid of MetallicGloss() call
// [SOURCE] UnityStandardCore.cginc
inline FragmentCommonData MetallicSetup_f3VC(half3 color)
{
	half oneMinusReflectivity;
	half3 specColor;
	half3 diffColor = DiffuseAndSpecularFromMetallic(color, _Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.smoothness = _Glossiness;
	return o;
}


// [RMS] this is same as 2017.3 FragmentSetup(), 
// but with extra diffuse color argument passed through to MetallicSetup,
// and all the btis we aren't using removed (eg anything based on texture coordinates)
// [SOURCE] UnityStandardCore.cginc
// parallax transformed texcoord is used to sample occlusion
inline FragmentCommonData FragmentSetup_f3VC(half3 i_eyeVec, half3 normalWorld, float3 i_posWorld, half3 color)
{
	half alpha = _Color.a;
#if defined(_ALPHATEST_ON)
	clip(alpha - _Cutoff);
#endif

	FragmentCommonData o = MetallicSetup_f3VC(color);
	o.normalWorld = normalize(normalWorld);
	o.eyeVec = NormalizePerPixelNormal(i_eyeVec);   // this doesn't normalize on Pre-SM3.0 [RMS]
	o.posWorld = i_posWorld;

	// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	o.diffColor = PreMultiplyAlpha(o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
	return o;
}



half3 backface_grid(float3 posWorld, half3 color1, half3 color2, float freq, int shift) {
	uint gridx = abs(int((posWorld.x+ shift) / freq));
	uint gridy = abs(int((posWorld.y+ shift) / freq));
	uint gridz = abs(int((posWorld.z+ shift) / freq));
	uint val = (gridx+gridy+gridz) % 2;
	return lerp(color1, color2, val);
}
half3 backface_scallop(float3 posWorld, half3 color1, half3 color2, float freq) {
	float wavepos = abs(posWorld.x + posWorld.y + posWorld.z);
	return lerp(color1, color2, fmod(wavepos, freq) / freq);
}
half3 backface_wave(float3 posWorld, half3 color1, half3 color2, float freq) {
	float wavepos = abs(posWorld.x + posWorld.y + posWorld.z);
	return lerp(color1, color2, (cos(freq*wavepos)+1.0)*0.5);
}


// convert linear index to (u,v) coordinates
// texel_size is {1/width, 1/height, width, height}
// https://docs.unity3d.com/Manual/SL-PropertiesInPrograms.html
float2 int_to_uv(uint index, float4 texel_size) {
	uint height = uint(texel_size.w);
	uint width = uint(texel_size.z);
	float index_uvy = (float(index / height) + 0.5) * texel_size.y;
	float index_uvx = (float(index % width) + 0.5) * texel_size.x;
	return float2(index_uvx, index_uvy);
}


// convert linear index to integer (x,y) coordinates (and 0 as mipmap)
// texel_size is {1/width, 1/height, width, height}
// https://docs.unity3d.com/Manual/SL-PropertiesInPrograms.html
uint3 int_to_intuv(uint index, float4 texel_size) {
	uint y = index / uint(texel_size.w);
	uint x = index % uint(texel_size.z);
	return uint3(x, y, 0);
}



float3 ray_plane_intersect(float3 ray_origin, float3 ray_direction, float3 plane_normal, float plane_d)
{
	float div = dot(ray_direction, plane_normal);
	float t = -( dot(ray_origin, plane_normal) - plane_d) / div;
	return ray_origin + t * ray_direction;
}



// [RMS] added multiply by color & alpha
half4 fragForwardBaseInternal_f3VC (VertexOutputForwardBase_f3VC i)
{
	// if we are back-facing, flip normal immediately, so that 
	// flipped normal goes into all the unity shading code
	half3 normal = i.tangentToWorldAndPackedData[2].xyz;
	float side = sign(dot(normal, i.eyeVec));	// positive is backface...
	normal = -side * normal;
	float backface_t = (side + 1.0) * 0.5;

	// these are inputs
	float3 posWorld = IN_WORLDPOS(i);
	half3 color = _Color;

	UnityLight mainLight = MainLight();

	// if we have clip plane, 
	if (_EnableClipPlane > 0) {
		float d = dot(posWorld.xyz, _ClipPlaneEquation.xyz);
		if (d > _ClipPlaneEquation.w) {
			discard;
		}
		if (_EnableClipPlane > 1 && backface_t > 0) {
			normal = _ClipPlaneEquation.xyz;
			float diffuse = dot(normal, mainLight.dir);
			// [RMS] this gives a alternating-lines effect, but it slides around because
			// we aren't actually transforming back to scene, just relative to origin
			//float3 raydir = normalize(posWorld - _WorldSpaceCameraPos);
			//float3 planePos = ray_plane_intersect(_WorldSpaceCameraPos, raydir, normal, _ClipPlaneEquation.w);
			//planePos -= _SceneOriginWorld.xyz;
			//diffuse *= abs(sin(2 * planePos.x)) < 0.5 ? 1 : 0.8;

			half4 clipcolor = _ClipPlaneColor;
			clipcolor.rgb *= diffuse;
			return clipcolor;
		}
	}

	// replace color with vertex color
#ifdef _ENABLE_VERTEX_COLOR
	color = i.vtx_color;    // should we multiply? or, allow multiply?
#endif

	// replace color with group color
#ifdef _FACE_GROUP_COLORS
	float4 gid_col = _FaceIndexMap.Load(int_to_intuv(i.tri_id, _FaceIndexMap_TexelSize));
	uint group_id = (uint)gid_col.x;
	float4 group_color = _GroupColorMap.Load(int_to_intuv(group_id, _GroupColorMap_TexelSize));
	color = group_color.rgb;
#endif

	// apply back-face stylized shading
#ifdef _BACKFACE_GRID
	half3 backface_color = backface_grid(posWorld, _BackFaceColor, _BackFaceColor2, _BackFaceFreq, 1000);
	color = lerp(color, backface_color, backface_t);
#endif
#ifdef _BACKFACE_SCALLOP
	half3 backface_color = backface_scallop(posWorld, _BackFaceColor, _BackFaceColor2, _BackFaceFreq);
	color = lerp(color, backface_color, backface_t);
#endif
#ifdef _BACKFACE_WAVE
	half3 backface_color = backface_wave(posWorld, _BackFaceColor, _BackFaceColor2, _BackFaceFreq);
	color = lerp(color, backface_color, backface_t);
#endif

	// simplified unity standard setup code, with our color
	//FRAGMENT_SETUP(s);
	FragmentCommonData s = FragmentSetup_f3VC(i.eyeVec, normal, posWorld, color);

	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

	// (end of standard unity stuff)

	// zero out specular/smoothness for back-faces
	s.specColor = lerp(s.specColor, 0, backface_t);
	s.smoothness = lerp(s.smoothness, 0, backface_t);

	// now do some more standard unity stuff...

	// [RMS] this is not zero...but is likely doing more work than we need to do, since
	//   we have disabled most GI features?
	UnityGI gi = FragmentGI(s, 1.0, 0, atten, mainLight);

	// [RMS] this is probably a more capable BRDF than we really need?
	half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);

	// add emission
	half4 emission = lerp(_EmissionColor, _BackfaceEmissionColor, backface_t);
	c.rgb += emission.rgb * emission.a;

	// apply fog
	UNITY_APPLY_FOG(i.fogCoord, c.rgb);

	// this is output color
	half4 fwd_color = OutputForward(c, s.alpha);

	if (_Wireframe > 0) {
		// [TODO] should be doing proper blending here?? is weird if wireframe alpha is not 1
		float edge_dist = min(i.dist.x, min(i.dist.y, i.dist.z));		// distance from frag to nearest edge
		float t = clamp(edge_dist / _WireWidth, 0, 1);
		t = 1 - (t*t);
		t = t * t*t*_WireColor.a;
		fwd_color.rgb = lerp(fwd_color.rgb, _WireColor.rgb, t);
	}

	return fwd_color;
}

half4 fragForwardBase_f3VC (VertexOutputForwardBase_f3VC i) : SV_Target	// backward compatibility (this used to be the fragment entry function)
{
	return fragForwardBaseInternal_f3VC(i);
}




/*
 * Forward Additive pass
 *  [TODO] need to get rid of all the stuff we aren't using...
 */

// [RMS] don't think I added anything here...
VertexOutputForwardAdd vertForwardAdd_f3VC(VertexInput v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputForwardAdd o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	o.posWorld = posWorld.xyz;
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
#else
	o.tangentToWorldAndLightDir[0].xyz = 0;
	o.tangentToWorldAndLightDir[1].xyz = 0;
	o.tangentToWorldAndLightDir[2].xyz = normalWorld;
#endif
	//We need this for shadow receiving
	UNITY_TRANSFER_SHADOW(o, v.uv1);

	float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
#ifndef USING_DIRECTIONAL_LIGHT
	lightDir = NormalizePerVertexNormal(lightDir);
#endif
	o.tangentToWorldAndLightDir[0].w = lightDir.x;
	o.tangentToWorldAndLightDir[1].w = lightDir.y;
	o.tangentToWorldAndLightDir[2].w = lightDir.z;

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
#endif

	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}


half4 fragForwardAdd_f3VC(VertexOutputForwardAdd i) : SV_Target
{
	UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	FRAGMENT_SETUP_FWDADD(s)

	// if we are back-facing, flip normal immediately, so that 
	// flipped normal goes into all the unity shading code
	float side = sign(dot(s.normalWorld, s.eyeVec));	// positive is backface...
	if (side > 0) {
		s.normalWorld = -s.normalWorld;
		//discard;
	}
	// handle clipping plane
	if (_EnableClipPlane > 0) {
		float d = dot(s.posWorld.xyz, _ClipPlaneEquation.xyz);
		if (d > _ClipPlaneEquation.w) {
			discard;
		}
		if (_EnableClipPlane > 1 && side > 0) {
			discard;		// in fill mode, just skip additive pass, too hard to set up
		}
	}

	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
	UnityLight light = AdditiveLight(IN_LIGHTDIR_FWDADD(i), atten);
	UnityIndirect noIndirect = ZeroIndirect();

	half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, noIndirect);

	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0, 0, 0, 0)); // fog towards black in additive pass
	return OutputForward(c, s.alpha);
}









#endif