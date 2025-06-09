//-------------------------------------最終カラー計算
	       OUT.rgb      = Color * Lighting;
	       OUT.rgb      = lerp(OUT.rgb , Color , _Unlit);
	       OUT.rgb      = lerp(OUT.rgb , Reflection , _Metallic * ReflectMask);
	       OUT.rgb     += Specular;
	       OUT.rgb     += MatCapture;

//----リムライティング混合
	if (_RimLitEnable) {
		if (_RimLitMode == 0) OUT.rgb += RimLight;
		if (_RimLitMode == 1) OUT.rgb *= RimLight;
		if (_RimLitMode == 2) OUT.rgb  = saturate(OUT.rgb - RimLight);
	}

//----デカールエミッション混合
	if (_DecalEnable) {
		#ifdef PASS_FA
			if ((_DecalMode == 4) | (_DecalMode == 5)) DecalColor.rgb *= LightPower;
		#endif
		if (_DecalMode == 4) OUT.rgb += DecalColor.rgb * _DecalEmission;
		if (_DecalMode == 5) OUT.rgb += DecalColor.rgb * _DecalEmission;
	}

//----エミッション混合
	if (_EmissionEnable) {

		float EmissionRev   = MonoColor(LightBase);
		#ifdef PASS_FB
			EmissionRev += MonoColor(SHLightBase) + MonoColor(VLightBase);
		#endif

		      EmissionRev   = 1.0f - pow(saturate(EmissionRev) , 0.44964029f);
		      EmissionRev   = saturate((EmissionRev - _EmissionInTheDark + 0.1f) * 10.0f);
		      Emission     *= EmissionRev;

		if (_EmissionMode == 0) OUT.rgb += Emission;
		if (_EmissionMode == 1) {
			OUT.rgb *= saturate(1.0f - Emission);
			OUT.rgb += (lerp(Color , Reflection , (_Metallic * ReflectMask)) + ((Specular + MatCapture) * SpecularMask)) * Emission;
		}
		if (_EmissionMode == 2) OUT.rgb  = saturate(OUT.rgb - Emission);
	}

//----視差エミッション混合
	if (_ParallaxEnable) {

		float ParallaxRev   = MonoColor(LightBase);
		#ifdef PASS_FB
			ParallaxRev += MonoColor(SHLightBase) + MonoColor(VLightBase);
		#endif

		      ParallaxRev   = 1.0f - pow(saturate(ParallaxRev) , 0.44964029f);
		      ParallaxRev   = saturate((ParallaxRev - _ParallaxInTheDark + 0.1f) * 10.0f);
		      Parallax     *= ParallaxRev;

		if (_ParallaxMode == 0) OUT.rgb += Parallax;
		if (_ParallaxMode == 1) {
			OUT.rgb *= saturate(1.0f - Parallax);
			OUT.rgb += (lerp(Color , Reflection , (_Metallic * ReflectMask)) + ((Specular + MatCapture) * SpecularMask)) * Parallax;
		}
		if (_ParallaxMode == 2) OUT.rgb  = saturate(OUT.rgb - Parallax);
	}

//----オクルージョン
	if (_OcclusionMode == 2) OUT.rgb *= lerp(1.0f , UNITY_SAMPLE_TEX2D_SAMPLER(_OcclusionMap , _MainTex , SubUV).rgb , _OcclusionStrength);

//----エミッションのテクスチャアルファ無視
	#ifdef TRANSPARENT

		if (_EmissionEnable & _IgnoreTexAlphaE) {
			float EmissionAlpha    = MonoColor(Emission);
			OUT.a = saturate(OUT.a + EmissionAlpha  );
		}

//----視差エミッションのテクスチャアルファ無視
		if (_ParallaxEnable & _IgnoreTexAlphaPE) {
			float ParallaxAlpha    = MonoColor(Parallax);
			OUT.a = saturate(OUT.a + ParallaxAlpha  );
		}

//----リフレクションのテクスチャアルファ無視
		if (_ReflectionEnable & _IgnoreTexAlphaR) {
			float ReflectionAlpha  = 0.0f;
			      ReflectionAlpha += MonoColor(Reflection) * ReflectMask * _Metallic;
			      ReflectionAlpha += MonoColor(Specular)   * SpecularMask;
			      ReflectionAlpha += MonoColor(MatCapture) * ReflectMask;
			OUT.a = saturate(OUT.a + ReflectionAlpha);
		}

//----リムライトのテクスチャアルファ無視
		if (_RimLitEnable & _IgnoreTexAlphaRL) {
			float RimLightAlpha    = MonoColor(RimLight);
			OUT.a = saturate(OUT.a + RimLightAlpha  );
		}

	#endif
