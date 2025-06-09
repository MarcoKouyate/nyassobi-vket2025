//--------------------------------------------------------------
//              Sunao Shader Core
//                      Copyright (c) 2022 揚茄子研究所
//--------------------------------------------------------------


//-------------------------------------Include

	#include "UnityCG.cginc"
	#include "AutoLight.cginc"
	#include "Lighting.cginc"

//-------------------------------------変数宣言

//----Main
	UNITY_DECLARE_TEX2D(_MainTex);
	float4    _MainTex_ST;
	float4    _Color;
	float     _Cutout;
	float     _Alpha;
	sampler2D _BumpMap;
	float4    _BumpMap_ST;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_OcclusionMap);
	UNITY_DECLARE_TEX2D_NOSAMPLER(_AlphaMask);
	UNITY_DECLARE_TEX2D_NOSAMPLER(_SubTex);
	float4    _SubTex_ST;
	float4    _SubTex_TexelSize;
	float4    _SubColor;
	bool      _SubTexEnable;
	float     _SubTexBlend;
	uint      _SubTexBlendMode;
	uint      _SubTexCulling;
	float     _Bright;
	float     _BumpScale;
	float     _OcclusionStrength;
	float     _OcclusionMode;
	float     _AlphaMaskStrength;
	bool      _VertexColor;
	float     _UVScrollX;
	float     _UVScrollY;
	float     _UVAnimation;
	uint      _UVAnimX;
	uint      _UVAnimY;
	bool      _UVAnimOtherTex;
	bool      _DecalEnable;
	sampler2D _DecalTex;
	float4    _DecalTex_TexelSize;
	float4    _DecalColor;
	float     _DecalPosX;
	float     _DecalPosY;
	float     _DecalSizeX;
	float     _DecalSizeY;
	float     _DecalRotation;
	uint      _DecalMode;
	uint      _DecalMirror;
	float     _DecalEmission;
	float     _DecalBright;
	float     _DecalScrollX;
	float     _DecalScrollY;
	float     _DecalAnimation;
	uint      _DecalAnimX;
	uint      _DecalAnimY;

//----Shading & Lighting
	UNITY_DECLARE_TEX2D_NOSAMPLER(_ShadeMask);
	float     _Shade;
	float     _ShadeWidth;
	float     _ShadeGradient;
	float     _ShadeColor;
	float4    _CustomShadeColor;
	bool      _ToonEnable;
	uint      _Toon;
	float     _ToonSharpness;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_LightMask);
	float     _LightBoost;
	float     _Unlit;
	float     _MonochromeLit;
	uint      _LightDirMode;
	float     _CustomLightRotX;
	float     _CustomLightRotY;

//----Outline
	bool      _OutLineEnable;
	sampler2D _OutLineMask;
	float4    _OutLineColor;
	float     _OutLineSize;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_OutLineTexture);
	bool      _OutLineLighthing;
	bool      _OutLineTexColor;
	bool      _OutLineFixScale;

//----Emission
	bool      _EmissionEnable;
	sampler2D _EmissionMap;
	float4    _EmissionMap_ST;
	float4    _EmissionColor;
	float     _Emission;
	sampler2D _EmissionMap2;
	float4    _EmissionMap2_ST;
	uint      _EmissionMode;
	float     _EmissionBlink;
	float     _EmissionFrequency;
	uint      _EmissionWaveform;
	float     _EmissionScrX;
	float     _EmissionScrY;
	float     _EmissionAnimation;
	uint      _EmissionAnimX;
	uint      _EmissionAnimY;
	bool      _EmissionLighting;
	bool      _IgnoreTexAlphaE;
	float     _EmissionInTheDark;

//----Parallax Emission
	bool      _ParallaxEnable;
	sampler2D _ParallaxMap;
	float4    _ParallaxMap_ST;
	float4    _ParallaxColor;
	float     _ParallaxEmission;
	float     _ParallaxDepth;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_ParallaxDepthMap);
	float4    _ParallaxDepthMap_ST;
	sampler2D _ParallaxMap2;
	float4    _ParallaxMap2_ST;
	uint      _ParallaxMode;
	float     _ParallaxBlink;
	float     _ParallaxFrequency;
	uint      _ParallaxWaveform;
	float     _ParallaxPhaseOfs;
	float     _ParallaxScrX;
	float     _ParallaxScrY;
	float     _ParallaxAnimation;
	uint      _ParallaxAnimX;
	uint      _ParallaxAnimY;
	bool      _ParallaxLighting;
	bool      _IgnoreTexAlphaPE;
	float     _ParallaxInTheDark;

//----Reflection
	bool      _ReflectionEnable;
	sampler2D _MetallicGlossMap;
	float3    _GlossColor;
	float     _Specular;
	float     _Metallic;
	float     _GlossMapScale;
	sampler2D _MatCap;
	float3    _MatCapColor;
	bool      _MatCapMaskEnable;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_MatCapMask);
	float     _MatCapStrength;
	bool      _ToonGlossEnable;
	uint      _ToonGloss;
	bool      _SpecularTexColor;
	bool      _MetallicTexColor;
	bool      _MatCapTexColor;
	bool      _SpecularSH;
	float     _SpecularMask;
	uint      _ReflectLit;
	uint      _MatCapLit;
	bool      _IgnoreTexAlphaR;

//----Rim Lighting
	bool      _RimLitEnable;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_RimLitMask);
	float     _RimLit;
	float     _RimLitGradient;
	float4    _RimLitColor;
	bool      _RimLitLighthing;
	bool      _RimLitTexColor;
	uint      _RimLitMode;
	bool      _IgnoreTexAlphaRL;

//----Other
	uint      _Culling;
	bool      _AlphaToMask;
	float     _DirectionalLight;
	float     _PointLight;
	float     _SHLight;
	bool      _LightLimitter;
	float     _MinimumLight;
	int       _BlendOperation;

	bool      _EnableGammaFix;
	float     _GammaR;
	float     _GammaG;
	float     _GammaB;

	bool      _EnableBlightFix;
	float     _BlightOutput;
	float     _BlightOffset;

	bool      _LimitterEnable;
	float     _LimitterMax;


//-------------------------------------頂点シェーダ入力構造体

struct VIN {
	float4 vertex  : POSITION;
	float2 uv      : TEXCOORD;
	float2 uv1     : TEXCOORD1;
	float3 normal  : NORMAL;
	float4 tangent : TANGENT;
	float3 color   : COLOR;
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


//-------------------------------------頂点シェーダ出力構造体

struct VOUT {

	nointerpolation float4 pos     : SV_POSITION;
	                float4 vertex  : VERTEX;
	                float3 wpos    : WORLDPOS;
	nointerpolation float3 campos  : CAMERAPOS0;
	                float2 uv      : TEXCOORD0;
	nointerpolation float4 uvanm   : TEXANIM;
	                float4 decal   : DECAL0;
	                float4 decal2  : DECAL1;
	nointerpolation float4 decanm  : DECAL2;
	                float3 normal  : NORMAL;
	                float3 color   : COLOR0;
	                float4 tangent : TANGENT0;
	                float3 bitan   : TANGENT1;
	                float3 ldir    : LIGHT0;
	nointerpolation float4 toon    : TOON;
	nointerpolation float3 vfront  : VFRONT;
	                float4 euv     : EMISSION0;
	nointerpolation float3 eprm    : EMISSION1;
	                float4 peuv    : EMISSION2;
	                float2 pduv    : EMISSION3;
	nointerpolation float3 peprm   : EMISSION4;
	                float2 pview   : EMISSION5;

	#ifdef PASS_FB
		nointerpolation float3 shdir   : LIGHT1;
		nointerpolation float3 shmax   : COLOR1;
		nointerpolation float3 shmin   : COLOR2;
		                float4 vldirX  : LIGHT2;
		                float4 vldirY  : LIGHT3;
		                float4 vldirZ  : LIGHT4;
		                float4 vlcorr  : LIGHT5;
		                float4 vlatn   : LIGHT6;
	#endif

	#ifdef PASS_FA
		UNITY_LIGHTING_COORDS(1 , 2)
	#endif

	UNITY_FOG_COORDS(3)

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

	#include "SunaoShader_Function.cginc"

//-------------------------------------頂点シェーダ

	#include "SunaoShader_Vert.cginc"

//-------------------------------------フラグメントシェーダ

	#include "SunaoShader_Frag.cginc"
