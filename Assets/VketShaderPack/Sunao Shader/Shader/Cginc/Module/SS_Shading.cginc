//-------------------------------------シェーディング
	float3 ShadeMask    = UNITY_SAMPLE_TEX2D_SAMPLER(_ShadeMask , _MainTex , SubUV).rgb * _Shade;
	float3 LightBoost   = 1.0f + (UNITY_SAMPLE_TEX2D_SAMPLER(_LightMask , _MainTex , SubUV).rgb * (_LightBoost - 1.0f));

//----オクルージョン
	#ifndef FUR
		if (_OcclusionMode == 1) Color *= lerp(1.0f , UNITY_SAMPLE_TEX2D_SAMPLER(_OcclusionMap , _MainTex , SubUV).rgb , _OcclusionStrength);
	#endif

//----ディフューズ
	float  Diffuse      = DiffuseCalc(Normal , IN.ldir , _ShadeGradient , _ShadeWidth);

	#ifdef PASS_FB
		float  SHDiffuse    = DiffuseCalc(Normal , IN.shdir , _ShadeGradient , _ShadeWidth);

		float4 VLDiffuse    = IN.vldirX * Normal.x;
		       VLDiffuse   += IN.vldirY * Normal.y;
		       VLDiffuse   += IN.vldirZ * Normal.z;
		       VLDiffuse    = max((float4)0.0f , VLDiffuse * IN.vlcorr);
	#endif

//----トゥーンシェーディング
	if (_ToonEnable) {
		Diffuse   = ToonCalc(Diffuse , IN.toon);
		#ifdef PASS_FB
			SHDiffuse = ToonCalc(SHDiffuse , IN.toon);
			VLDiffuse = ToonCalc(VLDiffuse , IN.toon);
		#endif
	}

//----影の色
	float3 ShadeColor   = saturate(Color * 3.0f - 1.5f) * _ShadeColor;
	       ShadeColor   = lerp(ShadeColor , _CustomShadeColor.rgb , _CustomShadeColor.a);
