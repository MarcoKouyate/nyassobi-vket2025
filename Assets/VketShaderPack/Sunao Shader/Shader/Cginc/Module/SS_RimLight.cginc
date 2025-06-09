//-------------------------------------リムライティング
	float3 RimLight     = (float3)0.0f;
	if (_RimLitEnable) {
		       RimLight  = RimLightCalc(Normal , View , _RimLit , _RimLitGradient);
		       RimLight *= _RimLitColor.rgb * _RimLitColor.a * UNITY_SAMPLE_TEX2D_SAMPLER(_RimLitMask , _MainTex , SubUV).rgb;
		if (_RimLitTexColor ) RimLight *= Color;
		if (_RimLitLighthing) {
		       RimLight *= saturate(LightBase + SHLightBase + VLightBase);
		}
		#ifdef PASS_FA
			RimLight *= Lighting;
		#endif
	}
