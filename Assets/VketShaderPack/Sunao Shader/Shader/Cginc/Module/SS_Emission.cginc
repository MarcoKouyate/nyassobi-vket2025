//-------------------------------------エミッション
	float3 Emission     = (float3)0.0f;

	if (_EmissionEnable) {
		       Emission    = _Emission * _EmissionColor.rgb;
		       Emission   *= tex2D(_EmissionMap  , IN.euv.xy).rgb * tex2D(_EmissionMap  , IN.euv.xy).a * IN.eprm.x;
		       Emission   *= tex2D(_EmissionMap2 , IN.euv.zw).rgb * tex2D(_EmissionMap2 , IN.euv.zw).a;

		if (_EmissionLighting) {
			#ifdef PASS_FB
				Emission   *= saturate(MonoColor(LightBase) + MonoColor(SHLightBase) + MonoColor(VLightBase));
			#endif
			#ifdef PASS_FA
				Emission   *= saturate(MonoColor(LightBase));
			#endif
		}
		#ifdef PASS_FA
			Emission *= LightPower;
		#endif
	}

//-------------------------------------視差エミッション
	float3 Parallax     = (float3)0.0f;

	if (_ParallaxEnable) {
		float  Height      = (1.0f - MonoColor(UNITY_SAMPLE_TEX2D_SAMPLER(_ParallaxDepthMap , _MainTex , IN.pduv).rgb)) * _ParallaxDepth;
		float2 ParallaxUV  = IN.peuv.xy;
		       ParallaxUV -= IN.pview * Height * _ParallaxMap_ST.xy;
		       Parallax    = _ParallaxEmission * _ParallaxColor.rgb;
		       Parallax   *= tex2D(_ParallaxMap  , ParallaxUV).rgb * tex2D(_ParallaxMap  , ParallaxUV).a * IN.peprm.x;
		       Parallax   *= tex2D(_ParallaxMap2 , IN.peuv.zw).rgb * tex2D(_ParallaxMap2 , IN.peuv.zw).a;

		if (_ParallaxLighting) {
			#ifdef PASS_FB
				Parallax   *= saturate(MonoColor(LightBase) + MonoColor(SHLightBase) + MonoColor(VLightBase));
			#endif
			#ifdef PASS_FA
				Parallax   *= saturate(MonoColor(LightBase));
			#endif
		}
		#ifdef PASS_FA
			Parallax *= LightPower;
		#endif
	}
