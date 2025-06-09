//-------------------------------------リフレクション
	float  Smoothness   = 0.0f;
	float3 SpecularMask = (float3)0.0f;
	float3 ReflectMask  = (float3)0.0f;
	float  MatCapSmooth = 0.0f;
	float3 MatCapMask   = (float3)0.0f;
	float3 Specular     = (float3)0.0f;
	float3 Reflection   = (float3)0.0f;
	float3 MatCapture   = (float3)0.0f;

	if (_ReflectionEnable) {
//----スペキュラ反射
		       Smoothness   = tex2D(_MetallicGlossMap , SubUV).a * _GlossMapScale;
		       SpecularMask = tex2D(_MetallicGlossMap , SubUV).rgb;
		       SpecularMask = lerp(1.0f , SpecularMask , _SpecularMask);

		float3 RLSpecular   = SpecularCalc(Normal , IN.ldir , View , Smoothness);
		float3 SHSpecular   = (float3)0.0f;
		float  SpecularInt  = _Specular * ((Smoothness * Smoothness * Smoothness) + 0.25f);

		#ifdef PASS_FB
			if (_SpecularSH) SHSpecular = SpecularCalc(Normal , IN.shdir , View , Smoothness);
		#endif
		       RLSpecular  *= SpecularInt;
		       SHSpecular  *= SpecularInt;

//----トゥーンスペキュラ
		if (_ToonGlossEnable) {
		       RLSpecular   = ToonCalc(RLSpecular , Toon(_ToonGloss , 0.75f));
		       SHSpecular   = ToonCalc(SHSpecular , Toon(_ToonGloss , 0.75f));
		}
//----
		       Specular     = RLSpecular * LightBase + SHSpecular * SHLightBase;

//----環境マッピング
		       ReflectMask  = tex2D(_MetallicGlossMap , SubUV).rgb;
		       Reflection   = ReflectionCalc(WorldPos , Normal , View , Smoothness);

		if (_ReflectLit == 1) Reflection *= saturate(  LightBase + VLightBase);
		if (_ReflectLit == 2) Reflection *= saturate(SHLightBase);
		if (_ReflectLit == 3) Reflection *= saturate(  LightBase + SHLightBase + VLightBase);

//----マットキャップ
		       MatCapSmooth = UNITY_SAMPLE_TEX2D_SAMPLER(_MatCapMask , _MainTex , IN.uv).a;
		       MatCapMask   = UNITY_SAMPLE_TEX2D_SAMPLER(_MatCapMask , _MainTex , IN.uv).rgb;
		       MatCapSmooth = lerp(MatCapSmooth , tex2D(_MetallicGlossMap , SubUV).a , _MatCapMaskEnable);
		       MatCapMask   = lerp(MatCapMask   , ReflectMask                        , _MatCapMaskEnable);

		float3 MatCapV      = normalize(IN.vfront - View * dot(View, IN.vfront));
		float3 MatCapH      = normalize(cross(View , MatCapV));

		float2 MatCapUV     = float2(dot(MatCapH , Normal), dot(MatCapV , Normal)) * 0.5f + 0.5f;
		       MatCapture   = tex2Dbias(_MatCap , float4(MatCapUV , 0.0f , 3.0f * (1.0f - MatCapSmooth))).rgb * _MatCapStrength;

		if (_MatCapLit == 1) MatCapture *= saturate(  LightBase + VLightBase);
		if (_MatCapLit == 2) MatCapture *= saturate(SHLightBase);
		if (_MatCapLit == 3) MatCapture *= saturate(  LightBase + SHLightBase + VLightBase);

//----
	       Specular     = Specular   * SpecularMask * _GlossColor;
	       Reflection   = Reflection * SpecularMask * _GlossColor;
	       MatCapture   = MatCapture * MatCapMask   * _MatCapColor;

		#ifdef PASS_FA
			Reflection *= LightPower;
			MatCapture *= LightPower;
		#endif

		if (_SpecularTexColor ) Specular   *= Color;
		if (_MetallicTexColor ) Reflection *= Color;
		if (_MatCapTexColor   ) MatCapture *= Color;
	}
