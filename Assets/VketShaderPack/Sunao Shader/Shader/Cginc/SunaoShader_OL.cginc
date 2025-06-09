//--------------------------------------------------------------
//              Sunao Shader Outline
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
	float     _AlphaMaskStrength;
	float     _Bright;
	bool      _VertexColor;
	float     _UVScrollX;
	float     _UVScrollY;
	float     _UVAnimation;
	uint      _UVAnimX;
	uint      _UVAnimY;
	bool      _UVAnimOtherTex;

//----Lighting
	float     _Unlit;
	float     _MonochromeLit;

//----Outline
	bool      _OutLineEnable;
	sampler2D _OutLineMask;
	float4    _OutLineColor;
	float     _OutLineSize;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_OutLineTexture);
	bool      _OutLineLighthing;
	bool      _OutLineTexColor;
	bool      _OutLineFixScale;

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
	nointerpolation float4 pos     : SV_POSITION;
	                float3 wpos    : WPOSITION;
	                float2 uv      : TEXCOORD0;
	                float3 color   : COLOR0;
	                float3 light   : LIGHT0;
	                float  mask    : MASK0;

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

//-------------------------------------アウトラインマスク
	o.mask    = MonoColor(tex2Dlod(_OutLineMask , float4(o.uv , 0.0f , 0.0f)).rgb) * saturate(_OutLineSize * 10000.0f);

//-------------------------------------頂点座標変換
	if (_OutLineEnable) {
		float3 VertexAdd;
		VertexAdd     = v.normal.xyz * GetScale(_OutLineSize , _OutLineFixScale);
		VertexAdd    *= o.mask;
		v.vertex.xyz += VertexAdd;
	} else {
		v.vertex.xyz  = (float3)0.0f;
	}

	o.pos     = UnityObjectToClipPos(v.vertex);
	o.wpos    = mul(unity_ObjectToWorld , v.vertex).xyz;

//-------------------------------------カラー
	o.color   = _OutLineColor.rgb;

	if (_OutLineTexColor) {
		if (_VertexColor) o.color *= v.color;
		                  o.color *= _Bright;
	}

//-------------------------------------ライティング
	o.light   = (float3)0.0f;

	if (_OutLineLighthing) {

		#ifdef PASS_FB
			o.light  =                  ShadeSH9(float4(-1.0f ,  0.0f ,  0.0f , 1.0f));
			o.light  = max(o.light , ShadeSH9(float4( 1.0f ,  0.0f ,  0.0f , 1.0f)));
			o.light  = max(o.light , ShadeSH9(float4( 0.0f , -1.0f ,  0.0f , 1.0f)));
			o.light  = max(o.light , ShadeSH9(float4( 0.0f ,  1.0f ,  0.0f , 1.0f)));
			o.light  = max(o.light , ShadeSH9(float4( 0.0f ,  0.0f , -1.0f , 1.0f)));
			o.light  = max(o.light , ShadeSH9(float4( 0.0f ,  0.0f ,  1.0f , 1.0f)));
			o.light *= _SHLight;

			o.light += _LightColor0 * _DirectionalLight;

			#if VERTEXLIGHT_ON
				float4 VLDirX = unity_4LightPosX0 - o.wpos.x;
				float4 VLDirY = unity_4LightPosY0 - o.wpos.y;
				float4 VLDirZ = unity_4LightPosZ0 - o.wpos.z;

				float4 VLLength = VLightLength(VLDirX , VLDirY , VLDirZ);

				float4 VLAtten  = VLightAtten(VLLength) * _PointLight;
				       VLAtten  *= 0.5f;
				       o.light += unity_LightColor[0].rgb * VLAtten.x;
				       o.light += unity_LightColor[1].rgb * VLAtten.y;
				       o.light += unity_LightColor[2].rgb * VLAtten.z;
				       o.light += unity_LightColor[3].rgb * VLAtten.w;
			#endif
			
			o.light = max(o.light , _MinimumLight);
		#endif
	}

//-------------------------------------ポイントライト
	#ifdef PASS_FA
		UNITY_TRANSFER_LIGHTING(o , v.uv1);
	#endif

//-------------------------------------フォグ
	UNITY_TRANSFER_FOG(o,o.pos);


	UNITY_TRANSFER_INSTANCE_ID(v , o);

	return o;
}


//----------------------------------------------------------------------
//                フラグメントシェーダ
//----------------------------------------------------------------------

float4 frag (VOUT IN) : COLOR {

	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

//-------------------------------------カラー計算
	float4 OUT       = UNITY_SAMPLE_TEX2D_SAMPLER(_OutLineTexture , _MainTex , IN.uv);
	       OUT.rgb  *= IN.color;
	if (_OutLineTexColor) OUT.rgb *= UNITY_SAMPLE_TEX2D(_MainTex , IN.uv);

	#if defined(TRANSPARENT) || defined(CUTOUT)
		OUT.a        = saturate(UNITY_SAMPLE_TEX2D(_MainTex , IN.uv).a * _Color.a * _Alpha);
		OUT.a       *= lerp(1.0f , MonoColor(UNITY_SAMPLE_TEX2D_SAMPLER(_AlphaMask , _MainTex , IN.uv).rgb) , _AlphaMaskStrength);
	#endif

//-------------------------------------カットアウト
		#include "./Module/SS_Cutout.cginc"

	clip(IN.mask - 0.1f);

//-------------------------------------ライティング
	float3 Lighting  = (float3)1.0f;

	#ifdef PASS_FB
		if (_OutLineLighthing) Lighting = IN.light;
	#endif
	#ifdef PASS_FA
		if (_OutLineLighthing) {
			UNITY_LIGHT_ATTENUATION(Atten , IN , IN.wpos);
			Lighting  = _LightColor0 * _PointLight * Atten * 0.6f;
		}
	#endif

	if (_LightLimitter) {
		float  MaxLight = 1.0f;
		       MaxLight = max(MaxLight , Lighting.r);
		       MaxLight = max(MaxLight , Lighting.g);
		       MaxLight = max(MaxLight , Lighting.b);
		       MaxLight = min(MaxLight , 1.25f);
		       Lighting = saturate(Lighting / MaxLight);
	}
	if (_MonochromeLit > 0.0f) Lighting = lerp(Lighting , MonoColor(Lighting) , _MonochromeLit);
	float  LightPower   = saturate(MonoColor(Lighting));

	OUT.rgb         *= Lighting;

	#include "./Module/SS_Output.cginc"


	return OUT;
}
