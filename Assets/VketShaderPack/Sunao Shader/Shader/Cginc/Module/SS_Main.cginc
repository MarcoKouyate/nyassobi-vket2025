//-------------------------------------メインカラー
	#if defined(TRANSPARENT) || defined(CUTOUT)
	       OUT.a        = saturate(UNITY_SAMPLE_TEX2D(_MainTex , MainUV).a * _Color.a * _Alpha);
	       OUT.a       *= lerp(1.0f , MonoColor(UNITY_SAMPLE_TEX2D_SAMPLER(_AlphaMask  , _MainTex , SubUV).rgb) , _AlphaMaskStrength);
	#endif

	float3 Color        = UNITY_SAMPLE_TEX2D(_MainTex , MainUV).rgb;
	       Color        = Color * _Color.rgb * _Bright * IN.color;

//-------------------------------------サブテクスチャ
	if (_SubTexEnable) {
		float4 SubTex    = UNITY_SAMPLE_TEX2D_SAMPLER(_SubTex , _MainTex , MainUV);
		       SubTex   *= _SubColor;
		       SubTex.a *= _SubTexBlend;
		if (_SubTexBlendMode == 1) SubTex.rgb *= Color.rgb;
		if (_SubTexBlendMode == 2) SubTex.rgb  = saturate(Color.rgb + SubTex.rgb);
		if (_SubTexBlendMode == 3) SubTex.rgb  = saturate(Color.rgb - SubTex.rgb);

		if (_SubTexCulling   == 1) SubTex.a   *=        Facing;
		if (_SubTexCulling   == 2) SubTex.a   *= 1.0f - Facing;
		       Color.rgb = lerp(Color.rgb , SubTex.rgb , SubTex.a);
		       OUT.a     = saturate(OUT.a + SubTex.a);
	}
