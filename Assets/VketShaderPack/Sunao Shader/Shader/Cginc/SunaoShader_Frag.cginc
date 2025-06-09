//--------------------------------------------------------------
//              Sunao Shader Fragment
//                      Copyright (c) 2022 揚茄子研究所
//--------------------------------------------------------------


float4 frag (VOUT IN , bool IsFrontFace : SV_IsFrontFace) : COLOR {

	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

//----ワールド座標
	float3 WorldPos     = mul(unity_ObjectToWorld , IN.vertex).xyz;

//----カメラ視点方向
	float3 View         = normalize(IN.campos - WorldPos);

//----面の裏表
	float  Facing       = (float)IsFrontFace;

//----UV
	float2 MainUV       = (IN.uv + IN.uvanm.xy) * IN.uvanm.zw;
	       MainUV      += float2(_UVScrollX , _UVScrollY) * _Time.y;
	float2 SubUV        = IN.uv;
	if (_UVAnimOtherTex) SubUV = MainUV;

//-------------------------------------
	float4 OUT          = float4(0.0f , 0.0f , 0.0f , 1.0f);

	#include "./Module/SS_Main.cginc"
	#include "./Module/SS_Decal.cginc"
	#include "./Module/SS_Cutout.cginc"
	#include "./Module/SS_Normal.cginc"
	#include "./Module/SS_Shading.cginc"
	#include "./Module/SS_Lighting.cginc"
	#include "./Module/SS_Emission.cginc"
	#include "./Module/SS_Reflection.cginc"
	#include "./Module/SS_RimLight.cginc"
	#include "./Module/SS_FinalColor.cginc"
	#include "./Module/SS_Output.cginc"

//-------------------------------------
	return OUT;
}
