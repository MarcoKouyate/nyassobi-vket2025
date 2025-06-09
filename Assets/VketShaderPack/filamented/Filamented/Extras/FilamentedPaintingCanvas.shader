// A shader for displaying artwork in high quality, as though it
// were printed or painted onto a canvas. 
Shader "Silent/Filamented Painting Canvas"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        [Space]
        [NoScaleOffset][Normal] _BumpMap("Canvas Pattern Normal", 2D) = "bump" {}
        _BumpSize("Pattern Scale", Float) = 1
        _BumpScale("Pattern Intensity", Float) = 1
        _BumpScaleWarp("Main Texture Canvas Intensity", Float) = 1
        [Space]
        [NoScaleOffset]_MOESMap("Canvas MOES Map", 2D) = "white" {}
        _MetallicScale("Metallic", Range( 0 , 1)) = 0
        _OcclusionScale("Occlusion", Range( 0 , 1)) = 0
        _SmoothnessScale("Smoothness", Range( 0 , 1)) = 0
        [Space]
        _Emission("Emission Power", Float) = 0
        _EmissionColor("Emission Color", Color) = (1,1,1,1)
        [Space]
		[Header(System)]
        [Toggle(_LIGHTMAPSPECULAR)]_LightmapSpecular("Lightmap Specular", Range(0, 1)) = 1
        _LightmapSpecularMaxSmoothness("Lightmap Specular Max Smoothness", Range(0, 1)) = 1
        _ExposureOcclusion("Lightmap Occlusion Sensitivity", Range(0, 1)) = 0.2
        [Space]
        [KeywordEnum(None, SH, RNM, MonoSH)] _Bakery ("Bakery Mode", Int) = 0
        [HideInInspector]_RNM0("RNM0", 2D) = "black" {}
        [HideInInspector]_RNM1("RNM1", 2D) = "black" {}
        [HideInInspector]_RNM2("RNM2", 2D) = "black" {}
        [Toggle(_LTCGI)] _LTCGI ("LTCGI", Int) = 0
        [Space]
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode", Int) = 2
        [Toggle(_ALPHATEST_ON)]_AtoCmode("Cutout Transparency", Float) = 0

        [NonModifiableTextureData][HideInInspector] _DFG("DFG", 2D) = "white" {}
    }

    CGINCLUDE
    	// First, setup what Filamented does. 
    	// Filamented's behaviour is decided by the shading model and what material properties are defined.
    	// These are listed in FilamentMaterialInputs.
    	// You can set up and use anything in the initMaterials function.

		// SHADING_MODEL_CLOTH
		// SHADING_MODEL_SUBSURFACE
    	// These are *not* currently supported.

    	// SHADING_MODEL_SPECULAR_GLOSSINESS
    	// If this is not defined, the material will default to metallic/roughness workflow.

    	#define MATERIAL_HAS_NORMAL
    	// If this is not defined, normal maps won't be enabled.

    	#define MATERIAL_HAS_AMBIENT_OCCLUSION
    	// If this is not defined, occlusion won't be taken into account

    	#define MATERIAL_HAS_EMISSIVE
    	// If this is not defined, emission won't be taken into account

    	// MATERIAL_HAS_ANISOTROPY
    	// If this is set, the material will support anisotropy.

    	// MATERIAL_HAS_CLEAR_COAT 
    	// If this is set, the material will support clear coat.

        // HAS_ATTRIBUTE_COLOR
        // If this is not defined, vertex colour will not be available.

        #define USE_DFG_LUT
        // Whether to use the lookup texture for specular reflection calculation.
        // Requires a shader property _DFG to be present and filled.
    ENDCG

    CGINCLUDE
    #ifndef UNITY_PASS_SHADOWCASTER

    // Include common files. These will include the other files as needed.
    #include "Packages/s-ilent.filamented/Filamented/UnityLightingCommon.cginc"
    #include "Packages/s-ilent.filamented/Filamented/UnityStandardInput.cginc"
    #include "Packages/s-ilent.filamented/Filamented/UnityStandardConfig.cginc"
    #include "Packages/s-ilent.filamented/Filamented/UnityStandardCore.cginc"
	// Note: Unfortunately, Input is still needed due to some interdependancies with other Unity files.
	// This means that some properties will always be defined, even if they aren't used. 
	// In practise, this won't affect the final compilation, but it means you'll need to watch out for the names
	// of some common parameters. In this case, only MOESMap and some other properties are defined here because
	// they are already defined in Input. 

    // uniform sampler2D _MainTex;
    // uniform sampler2D _BumpMap;
    uniform sampler2D _MOESMap;
	// uniform half _BumpScale;

	uniform float4 _MainTex_TexelSize;

	uniform half _BumpSize;
	uniform half _BumpScaleWarp;
	uniform half _MetallicScale;
	uniform half _OcclusionScale;
	uniform half _SmoothnessScale;
	uniform half _Emission;
	// uniform half3 _EmissionColor;

	// Vertex functions are called from UnityStandardCore.
	// You can alter values here, or copy the function in and modify it.
	VertexOutputForwardBase vertBase (VertexInput v) { return vertForwardBase(v); }
	VertexOutputForwardAdd vertAdd (VertexInput v) { return vertForwardAdd(v); }

float4 bicubicFilter(sampler2D inTex, float2 texcoord, float4 texscale)
{
    //#if _BICUBIC
    texcoord *= texscale.zw;
    float fx = frac(texcoord.x);
    float fy = frac(texcoord.y);
    texcoord.x -= fx;
    texcoord.y -= fy;

    float4 xcubic = cubic(fx);
    float4 ycubic = cubic(fy);

    float4 c = float4(texcoord.x - 0.5, texcoord.x + 1.5, texcoord.y - 0.5, texcoord.y + 1.5);
    float4 s = float4(xcubic.x + xcubic.y, xcubic.z + xcubic.w, ycubic.x + ycubic.y, ycubic.z + ycubic.w);
    float4 offset = c + float4(xcubic.y, xcubic.w, ycubic.y, ycubic.w) / s;

    float4 sample0 = tex2D(inTex, float2(offset.x, offset.z) * texscale.xy);
    float4 sample1 = tex2D(inTex, float2(offset.y, offset.z) * texscale.xy);
    float4 sample2 = tex2D(inTex, float2(offset.x, offset.w) * texscale.xy);
    float4 sample3 = tex2D(inTex, float2(offset.y, offset.w) * texscale.xy);

    float sx = s.x / (s.x + s.y);
    float sy = s.z / (s.z + s.w);

    return lerp(
        lerp(sample3, sample2, sx),
        lerp(sample1, sample0, sx), sy);
    //#else
    return tex2D(inTex, texcoord);
    //#endif
}

// https://iquilezles.org/www/articles/biplanar/biplanar.htm
// "p" point being textured
// "n" surface normal at "p"
// "k" controls the sharpness of the blending in the transitions areas
// "s" texture sampler
float4 biplanar( sampler2D sam, float3 p, float3 n, float k )
{
    // grab coord derivatives for texturing
    float3 dpdx = ddx(p);
    float3 dpdy = ddy(p);
    n = abs(n);

    // determine major axis (in x; yz are following axis)
    int3 ma =  (n.x>n.y && n.x>n.z) ? int3(0,1,2) :
               (n.y>n.z)            ? int3(1,2,0) :
                                      int3(2,0,1) ;
    // determine minor axis (in x; yz are following axis)
    int3 mi =  (n.x<n.y && n.x<n.z) ? int3(0,1,2) :
               (n.y<n.z)            ? int3(1,2,0) :
                                      int3(2,0,1) ;
    // determine median axis (in x;  yz are following axis)
    int3 me = clamp(3 - mi - ma, 0, 2); 
    
    // project+fetch
    float4 x = tex2Dgrad( sam, float2(   p[ma.y],   p[ma.z]), 
                               float2(dpdx[ma.y],dpdx[ma.z]), 
                               float2(dpdy[ma.y],dpdy[ma.z]) );
    float4 y = tex2Dgrad( sam, float2(   p[me.y],   p[me.z]), 
                               float2(dpdx[me.y],dpdx[me.z]),
                               float2(dpdy[me.y],dpdy[me.z]) );
    
    // blend factors
    float2 w = float2(n[ma.x],n[me.x]);
    // make local support
    w = clamp( (w-0.5773)/(1.0-0.5773), 0.0, 1.0 );
    // shape transition
    w = pow( w, k/8.0 );
    // blend and return
    return (x*w.x + y*w.y) / (w.x + w.y);
}

	// The material function itself!  You can alter the code below to add extra properties. 
inline MaterialInputs MyMaterialSetup (inout float4 i_tex, float3 i_eyeVec, half3 i_viewDirForParallax, float4 tangentToWorld[3], float3 i_posWorld)
{   
	// Sample the UVs for the canvas bumps first. These are applied to the
	// surface normal, but also the texture sampling. 
    float3x3 tangentToWorldOnly = float3x3(tangentToWorld[0].xyz, tangentToWorld[1].xyz, tangentToWorld[2].xyz);
    float3 normal = mul ( float3( 0, 0, 1 ), tangentToWorldOnly );
    half3 normalTangent = UnpackScaleNormal(biplanar(_BumpMap, i_posWorld * _BumpSize, normal, 1.0), _BumpScale); 
    half4 packedMap = biplanar (_MOESMap, i_posWorld * _BumpSize, normal, 1.0);
	
    half4 baseColor = bicubicFilter (_MainTex, i_tex.xy + normalTangent * 0.01 * _BumpScaleWarp, _MainTex_TexelSize);

    half metallic = packedMap.x * _MetallicScale;
    half occlusion = lerp(1, packedMap.y, _OcclusionScale);
    half emissionMask = packedMap.z;
    half smoothness = packedMap.w * _SmoothnessScale; 

    MaterialInputs material = (MaterialInputs)0;
    initMaterial(material);
    material.baseColor = baseColor;
    material.metallic = metallic;
    material.roughness = computeRoughnessFromGlossiness(smoothness);
    material.normal = normalTangent;
    material.emissive.rgb = baseColor.rgb * emissionMask * _Emission * _EmissionColor;
    material.emissive.a = 1.0;
    material.ambientOcclusion = occlusion;
    return material;
}

half4 fragForwardBaseTemplate (VertexOutputForwardBase i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    ShadingParams shading = (ShadingParams)0;
    // Initialize shading with expected parameters
    computeShadingParamsForwardBase(shading, i);

    UNITY_LIGHT_ATTENUATION(atten, i, shading.position);

    #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
    GetBakedAttenuation(atten, i.ambientOrLightmapUV.xy, shading.position);
    #endif

    // Your material setup goes here.
    MaterialInputs material =
    MyMaterialSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX(i), i.tangentToWorldAndPackedData, IN_WORLDPOS(i));

    prepareMaterial(shading, material);

#if (defined(_NORMALMAP) && defined(NORMALMAP_SHADOW))
    float noise = noiseR2(i.pos.xy);
    float nmShade = NormalTangentShadow (i.tex, i.lightDirTS, noise);
    shading.attenuation = min(shading.attenuation, max(1-nmShade, 0));
#endif

    float4 c = evaluateMaterial (shading, material);

    UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
    UNITY_APPLY_FOG(_unity_fogCoord, c.rgb);
    return c;
}

half4 fragForwardAddTemplate (VertexOutputForwardAdd i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    ShadingParams shading = (ShadingParams)0;
    // Initialize shading with expected parameters
    computeShadingParamsForwardAdd(shading, i);

    UNITY_LIGHT_ATTENUATION(atten, i, shading.position);

    // Your material setup goes here.
    MaterialInputs material =
    MyMaterialSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX_FWDADD(i), i.tangentToWorldAndLightDir, IN_WORLDPOS_FWDADD(i));

    prepareMaterial(shading, material);

#if (defined(_NORMALMAP) && defined(NORMALMAP_SHADOW))
    float noise = noiseR2(i.pos.xy);
    float nmShade = NormalTangentShadow (i.tex, i.lightDirTS, noise);
    shading.attenuation = min(shading.attenuation, max(1-nmShade, 0));
#endif

    float4 c = evaluateMaterial (shading, material);

    UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
    UNITY_APPLY_FOG_COLOR(_unity_fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
    return c;
}

half4 fragBase (VertexOutputForwardBase i) : SV_Target { return fragForwardBaseTemplate(i); }
half4 fragAdd (VertexOutputForwardAdd i) : SV_Target { return fragForwardAddTemplate(i); }
    #endif 

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" "LTCGI" = "_LTCGI" }
        LOD 300

        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Cull [_CullMode]
            AlphaToMask [_AtoCmode]

            CGPROGRAM
            #pragma target 4.0

            // -------------------------------------

            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
            
            #pragma shader_feature_local _ _BAKERY_RNM _BAKERY_SH _BAKERY_MONOSH
            #pragma shader_feature_local _LTCGI

            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertBase
            #pragma fragment fragBase

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Additive forward pass (one light per pass)
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
            ZWrite Off
            ZTest Equal
            Cull [_CullMode]
            AlphaToMask [_AtoCmode]

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------


            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF

            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertAdd
            #pragma fragment fragAdd

            ENDCG
        }
        
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual
            Cull [_CullMode]

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------

            #ifndef UNITY_PASS_SHADOWCASTER
            #define UNITY_PASS_SHADOWCASTER
            #endif  

            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "Packages/s-ilent.filamented/Filamented/UnityStandardShadow.cginc"

            ENDCG
        }
        

        // Deferred not implemented
        UsePass "Standard/DEFERRED"

        // Meta not implemented
        UsePass "Standard/META"

    }

    FallBack "VertexLit"
}
