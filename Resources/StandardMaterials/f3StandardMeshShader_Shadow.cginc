// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)


// [RMS] To support clip plane, have to duplicate entire
// unity shadow pass, so that we can discard clipped fragments.
// Otherwise they will still cast shadows.
//
// [TODO] not sure if we are properly supporting stereo? 
// no #define's in fragment shader for stereo struct...


#ifndef F3_STANDARD_MESH_SHADER_SHADOW_INCLUDED
#define F3_STANDARD_MESH_SHADER_SHADOW_INCLUDED

// [RMS] added our clip plane variables
float _EnableClipPlane;
half4 _ClipPlaneColor;
float4 _ClipPlaneEquation;


#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
struct VertexOutputShadowCaster_f3VC
{
	V2F_SHADOW_CASTER_NOPOS

	float3 posWorld                 : TEXCOORD8;		// [RMS] added this

#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
		float2 tex : TEXCOORD1;

#if defined(_PARALLAXMAP)
	half3 viewDirForParallax : TEXCOORD2;
#endif
#endif
};
#endif


#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
struct VertexOutputStereoShadowCaster_f3VC
{
	UNITY_VERTEX_OUTPUT_STEREO

	float3 posWorld                 : TEXCOORD8;		// [RMS] added this
};
#endif



void vertShadowCaster_f3VC(VertexInput v
	, out float4 opos : SV_POSITION
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
	, out VertexOutputShadowCaster_f3VC o
#endif
#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
	, out VertexOutputStereoShadowCaster_f3VC os
#endif
)
{
	UNITY_SETUP_INSTANCE_ID(v);
#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(os);
#endif
	TRANSFER_SHADOW_CASTER_NOPOS(o, opos)
#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
		o.tex = TRANSFORM_TEX(v.uv0, _MainTex);

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
#endif
#endif

// [RMS] added this block to compute world position
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
	o.posWorld = mul(unity_ObjectToWorld, v.vertex);
#endif
#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
	os.posWorld = mul(unity_ObjectToWorld, v.vertex);
#endif

}






half4 fragShadowCaster_f3VC (UNITY_POSITION(vpos)
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    , VertexOutputShadowCaster_f3VC i
#endif
) : SV_Target
{

	// [RMS] all the additions here are just so we can do this discard test!
    #ifdef SHADER_API_D3D11
	if (_EnableClipPlane > 0) {
		float d = dot(i.posWorld.xyz, _ClipPlaneEquation.xyz);
		if (d > _ClipPlaneEquation.w) {
			discard;
		}
	}
    #endif

	// [RMS] everything after here is standard unity
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
        #if defined(_PARALLAXMAP) && (SHADER_TARGET >= 30)
            half3 viewDirForParallax = normalize(i.viewDirForParallax);
            fixed h = tex2D (_ParallaxMap, i.tex.xy).g;
            half2 offset = ParallaxOffset1Step (h, _Parallax, viewDirForParallax);
            i.tex.xy += offset;
        #endif

        half alpha = tex2D(_MainTex, i.tex).a * _Color.a;
        #if defined(_ALPHATEST_ON)
            clip (alpha - _Cutoff);
        #endif
        #if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
            #if defined(_ALPHAPREMULTIPLY_ON)
                half outModifiedAlpha;
                PreMultiplyAlpha(half3(0, 0, 0), alpha, SHADOW_ONEMINUSREFLECTIVITY(i.tex), outModifiedAlpha);
                alpha = outModifiedAlpha;
            #endif
            #if defined(UNITY_STANDARD_USE_DITHER_MASK)
                // Use dither mask for alpha blended shadows, based on pixel position xy
                // and alpha level. Our dither texture is 4x4x16.
                #ifdef LOD_FADE_CROSSFADE
                    #define _LOD_FADE_ON_ALPHA
                    alpha *= unity_LODFade.y;
                #endif
                half alphaRef = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25,alpha*0.9375)).a;
                clip (alphaRef - 0.01);
            #else
                clip (alpha - _Cutoff);
            #endif
        #endif
    #endif // #if defined(UNITY_STANDARD_USE_SHADOW_UVS)

    #ifdef LOD_FADE_CROSSFADE
        #ifdef _LOD_FADE_ON_ALPHA
            #undef _LOD_FADE_ON_ALPHA
        #else
            UnityApplyDitherCrossFade(vpos.xy);
        #endif
    #endif

    SHADOW_CASTER_FRAGMENT(i)
}

#endif // UNITY_STANDARD_SHADOW_INCLUDED
