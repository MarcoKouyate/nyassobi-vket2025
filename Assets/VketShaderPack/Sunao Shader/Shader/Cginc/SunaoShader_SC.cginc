//--------------------------------------------------------------
//              Sunao Shader ShadowCaster
//                      Copyright (c) 2022 揚茄子研究所
//--------------------------------------------------------------


//-------------------------------------Include

	#include "UnityCG.cginc"

//-------------------------------------変数宣言

	sampler2D _MainTex;
	float4    _MainTex_ST;
	float4    _Color;
	float     _Cutout;
	float     _Alpha;
	sampler2D _AlphaMask;
	float     _AlphaMaskStrength;
	float     _UVScrollX;
	float     _UVScrollY;
	float     _UVAnimation;
	uint      _UVAnimX;
	uint      _UVAnimY;
	bool      _UVAnimOtherTex;
	bool      _OutLineEnable;
	sampler2D _OutLineMask;
	float     _OutLineSize;
	bool      _OutLineFixScale;
	uint      _Culling;

//-------------------------------------頂点シェーダ入力構造体

struct VIN {
	float4 vertex      : POSITION;
	float2 uv          : TEXCOORD0;
	float3 normal      : NORMAL;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//-------------------------------------頂点シェーダ出力構造体

struct VOUT {
	float2 uv          : TEXCOORD0;
	float4 uvanm       : TEXCOORD1;
	float  vadd        : VERTEXADD;

	V2F_SHADOW_CASTER;

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

	#include "SunaoShader_Function.cginc"

//-------------------------------------頂点シェーダ

VOUT vert (VIN v) {

	VOUT o;

	UNITY_INITIALIZE_OUTPUT(VOUT , o);
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

//----視線
	float3 CamPos;
	#ifdef USING_STEREO_MATRICES
		CamPos = (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]) * 0.5f;
	#else
		CamPos = _WorldSpaceCameraPos;
	#endif

	float3 View   = CamPos - mul(unity_ObjectToWorld , v.vertex).xyz;

//----面の裏表
	float  Facing = saturate(saturate(1.0f - dot(v.normal , View)) * 10.0f - 0.5f);

//-------------------------------------UV
	o.uv      = (v.uv * _MainTex_ST.xy) + _MainTex_ST.zw;

//-------------------------------------UVアニメーション
	o.uvanm   = float4(0.0f , 0.0f , 1.0f , 1.0f);

	if (_UVAnimation > 0.0f) {
		o.uvanm.zw  = 1.0f / float2(_UVAnimX , _UVAnimY);

		float2 UVAnimSpeed    = _UVAnimation * _UVAnimY;
		       UVAnimSpeed.y *= -o.uvanm.w;

		o.uvanm.xy += floor(frac(UVAnimSpeed * _Time.y) * float2(_UVAnimX , _UVAnimY));
	}

//-------------------------------------頂点座標計算
	o.vadd = 0.0f;
	if (_OutLineEnable) {
		float  OutlineScale  = GetScale(_OutLineSize , _OutLineFixScale);
		       OutlineScale *= MonoColor(tex2Dlod(_OutLineMask , float4(o.uv , 0.0f , 0.0f)).rgb);
		       OutlineScale *= Facing;
		       v.vertex.xyz += v.normal * OutlineScale;
		       o.vadd        = saturate(OutlineScale * 10000.0f);
	}


	TRANSFER_SHADOW_CASTER(o)

	UNITY_TRANSFER_INSTANCE_ID(v , o);

	return o;
}


//-------------------------------------フラグメントシェーダ

float4 frag (VOUT IN , bool IsFrontFace : SV_IsFrontFace) : COLOR {

	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

//----面の裏表
	float  Facing = (float)IsFrontFace;

//-------------------------------------メイン
	float4 OUT    = (float4)1.0f;

	#if defined(TRANSPARENT) || defined(CUTOUT)
		float2 MainUV       = (IN.uv + IN.uvanm.xy) * IN.uvanm.zw;
		       MainUV      += float2(_UVScrollX , _UVScrollY) * _Time.y;
		float2 SubUV        = IN.uv;
		if (_UVAnimOtherTex) SubUV = MainUV;

	           OUT.a        = saturate(tex2D(_MainTex , MainUV).a * _Color.a * _Alpha);
	           OUT.a       *= lerp(1.0f , MonoColor(tex2D(_AlphaMask  , SubUV).rgb) , _AlphaMaskStrength);
	#endif

	float  Alpha  = OUT.a;

	if (_Culling == 2) Alpha *= saturate(IN.vadd +         Facing );
	if (_Culling == 1) Alpha *= saturate(IN.vadd + (1.0f - Facing));

	#ifdef TRANSPARENT
		Alpha -= 0.3f;
	#endif

	#ifdef CUTOUT
		Alpha -= _Cutout;
	#endif

	clip(Alpha - 0.0001f);


	SHADOW_CASTER_FRAGMENT(IN)
	
	return OUT;
}
