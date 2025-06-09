//--------------------------------------------------------------
//              Sunao Shader Fur
//                      Copyright (c) 2022 揚茄子研究所
//--------------------------------------------------------------


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
	float     _AlphaMaskStrength;
	bool      _VertexColor;
	float     _UVScrollX;
	float     _UVScrollY;
	float     _UVAnimation;
	uint      _UVAnimX;
	uint      _UVAnimY;
	bool      _UVAnimOtherTex;

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

//---- Fur
	bool      _FurEnable;
	sampler2D _FurMask;
	float4    _FurColor;
	float     _FurLength;
	float     _FurWidth;
	float     _FurRoughness;
	bool      _FurFixScale;
	bool      _FurTexColor;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_FurTex);
	uint      _FurShapeMode;
	sampler2D _FurShapeTex;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_FurDirection);
	float     _FurGravity;
	bool      _FurLighting;

//----Other
	bool      _AlphaToMask;
	float     _DirectionalLight;
	float     _SHLight;
	float     _PointLight;
	bool      _LightLimitter;
	float     _MinimumLight;

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
	float2 uv      : TEXCOORD0;
	float2 uv1     : TEXCOORD1;
	float3 normal  : NORMAL;
	float3 color   : COLOR;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};


//-------------------------------------頂点シェーダ出力構造体

struct VOUT {
	nointerpolation float4 pos         : SV_POSITION;
	                float4 vertex      : VERTEX;
	                float3 wpos        : WORLDPOS;
	                float2 uv          : TEXCOORD0;
	                float3 normal      : NORMAL;
	                float3 color       : COLOR0;
	                float3 ldir        : LIGHT0;
	nointerpolation float4 toon        : TOON;
	                float2 furuv       : FURUV;

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


//----------------------------------------------------------------------
//                頂点シェーダ
//----------------------------------------------------------------------

VOUT vert (VIN v) {

	VOUT o;

	UNITY_INITIALIZE_OUTPUT(VOUT , o);
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

//-------------------------------------UV
	o.uv      = (v.uv * _MainTex_ST.xy) + _MainTex_ST.zw;

	if (_UVAnimOtherTex) {
		float4 UVScr  = float4(0.0f , 0.0f , 1.0f , 1.0f);
		float4 UVAnim = float4(0.0f , 0.0f , 1.0f , 1.0f);

		if (_UVAnimation > 0.0f) {
			UVAnim.zw  = 1.0f / float2(_UVAnimX , _UVAnimY);

			float2 UVAnimSpeed    = _UVAnimation * _UVAnimY;
			       UVAnimSpeed.y *= -UVAnim.w;

			UVAnim.xy += floor(frac(UVAnimSpeed * _Time.y) * float2(_UVAnimX , _UVAnimY));
		}
		
		o.uv  = (o.uv + UVAnim.xy) * UVAnim.zw;
		o.uv += float2(_UVScrollX , _UVScrollY) * _Time.y;
	}

	o.furuv   = float2(0.0f , 1.0f);

//-------------------------------------頂点座標変換
	o.vertex  = v.vertex;
	o.pos     = UnityObjectToClipPos(o.vertex);
	o.wpos    = mul(unity_ObjectToWorld , o.vertex).xyz;
	o.normal  = v.normal;

//-------------------------------------頂点カラー
	o.color   = (float3)1.0f;
	if (_VertexColor) o.color = v.color;

//-------------------------------------ライト方向
	o.ldir    = _WorldSpaceLightPos0.xyz;

	#ifdef PASS_FA
		o.ldir -= o.wpos;
	#endif

	o.ldir    = normalize(o.ldir);

//-------------------------------------SHライト
	#ifdef PASS_FB
		float3 SHColor[6];
		SHColor[0]   = ShadeSH9(float4(-1.0f ,  0.0f ,  0.0f , 1.0f));
		SHColor[1]   = ShadeSH9(float4( 1.0f ,  0.0f ,  0.0f , 1.0f));
		SHColor[2]   = ShadeSH9(float4( 0.0f , -1.0f ,  0.0f , 1.0f));
		SHColor[3]   = ShadeSH9(float4( 0.0f ,  1.0f ,  0.0f , 1.0f));
		SHColor[4]   = ShadeSH9(float4( 0.0f ,  0.0f , -1.0f , 1.0f));
		SHColor[5]   = ShadeSH9(float4( 0.0f ,  0.0f ,  1.0f , 1.0f));

		float SHLength[6];
		SHLength[0]  = MonoColor(SHColor[0]);
		SHLength[1]  = MonoColor(SHColor[1]);
		SHLength[2]  = MonoColor(SHColor[2]);
		SHLength[3]  = MonoColor(SHColor[3]) + 0.000001f;
		SHLength[4]  = MonoColor(SHColor[4]);
		SHLength[5]  = MonoColor(SHColor[5]);

		o.shdir      = SHLightDirection(SHLength);
		o.shmax      = SHLightMax(SHColor) * _SHLight;
		o.shmin      = SHLightMin(SHColor) * _SHLight;

		if (_MonochromeLit > 0.0f) {
			o.shmax  = lerp(o.shmax , MonoColor(o.shmax) , _MonochromeLit);
			o.shmin  = lerp(o.shmin , MonoColor(o.shmin) , _MonochromeLit);
		}

		o.shmax      = max(o.shmax , _MinimumLight        );
		o.shmin      = max(o.shmin , _MinimumLight * 0.75f);

	#endif

//-------------------------------------Vertexライト
	#ifdef PASS_FB
		#if defined(UNITY_SHOULD_SAMPLE_SH) && defined(VERTEXLIGHT_ON)

			o.vldirX  = unity_4LightPosX0 - o.wpos.x;
			o.vldirY  = unity_4LightPosY0 - o.wpos.y;
			o.vldirZ  = unity_4LightPosZ0 - o.wpos.z;

			float4 VLLength = VLightLength(o.vldirX , o.vldirY , o.vldirZ);
			o.vlcorr  = rsqrt(VLLength);
			o.vlatn   = VLightAtten(VLLength) * _PointLight;

		#else

			o.vldirX  = (float4)0.0f;
			o.vldirY  = (float4)0.0f;
			o.vldirZ  = (float4)0.0f;
			o.vlcorr  = (float4)0.0f;
			o.vlatn   = (float4)0.0f;

		#endif
	#endif

//-------------------------------------ポイントライト
	#ifdef PASS_FA
		UNITY_TRANSFER_LIGHTING(o , v.uv1);
	#endif

//-------------------------------------Toon
	o.toon    = Toon(_Toon , _ToonSharpness);

//-------------------------------------フォグ
	UNITY_TRANSFER_FOG(o,o.pos);


	UNITY_TRANSFER_INSTANCE_ID(v , o);

	return o;
}

//----------------------------------------------------------------------
//                ジオメトリシェーダ
//----------------------------------------------------------------------

	#include "./SunaoShader_Geom.cginc"

//----------------------------------------------------------------------
//                フラグメントシェーダ
//----------------------------------------------------------------------

float4 frag (VOUT IN) : COLOR {

	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

//----面の裏表
	float  Facing       = 1.0f;

//----UV
	float2 MainUV       = IN.uv;
	float2 SubUV        = IN.uv;

//----ノーマル
	float3 Normal       = UnityObjectToWorldNormal(IN.normal);

//-------------------------------------
	float4 OUT          = float4(0.0f , 0.0f , 0.0f , 1.0f);

	#include "./Module/SS_Main.cginc"
	#include "./Module/SS_Fur.cginc"
	#include "./Module/SS_Shading.cginc"
	#include "./Module/SS_Lighting.cginc"

	#ifdef PASS_FA
		Lighting *= 0.2f;
	#endif

	       OUT.rgb      = Color * Lighting;
	       OUT.rgb      = lerp(OUT.rgb , Color , _Unlit);

	#include "./Module/SS_Output.cginc"

	return OUT;
}
