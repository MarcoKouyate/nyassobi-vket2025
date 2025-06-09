//-------------------------------------ライティング
	float3 LightBase;
	float3 VLightBase       = (float3)0.0f;
	float3 SHLightBase      = (float3)0.0f;

	#ifdef PASS_FB
		       LightBase    = _LightColor0 * _DirectionalLight;
		float3 VLight0      = unity_LightColor[0].rgb * IN.vlatn.x * 0.5f;
		float3 VLight1      = unity_LightColor[1].rgb * IN.vlatn.y * 0.5f;
		float3 VLight2      = unity_LightColor[2].rgb * IN.vlatn.z * 0.5f;
		float3 VLight3      = unity_LightColor[3].rgb * IN.vlatn.w * 0.5f;
		       VLightBase   = saturate(VLight0 + VLight1 + VLight2 + VLight3);
		       SHLightBase  = IN.shmax;
	#endif
	#ifdef PASS_FA
		       UNITY_LIGHT_ATTENUATION(Atten , IN , IN.wpos);
			   LightBase    = _LightColor0 * _PointLight * Atten * 0.6f;
	#endif

//----モノクロライティング
	if (_MonochromeLit > 0.0f) {
		LightBase  = lerp(LightBase , MonoColor(LightBase) , _MonochromeLit);
		#ifdef PASS_FB
			   VLight0      = lerp(VLight0    , MonoColor(VLight0)    , _MonochromeLit);
			   VLight1      = lerp(VLight1    , MonoColor(VLight1)    , _MonochromeLit);
			   VLight2      = lerp(VLight2    , MonoColor(VLight2)    , _MonochromeLit);
			   VLight3      = lerp(VLight3    , MonoColor(VLight3)    , _MonochromeLit);
			   VLightBase   = lerp(VLightBase , MonoColor(VLightBase) , _MonochromeLit);
		#endif
	}

//----ライト反映
	float3 Lighting;
	float3 DiffColor        = LightingCalc(LightBase , Diffuse , ShadeColor , ShadeMask);

	#ifdef PASS_FB
		float3 SHDiffColor  = LightingCalc(SHLightBase , SHDiffuse , ShadeColor , ShadeMask);
		       SHDiffColor  = saturate(SHDiffColor - IN.shmin) + IN.shmin;
	
		#ifndef FUR
			if (_OcclusionMode == 0) SHDiffColor *= lerp(1.0f , UNITY_SAMPLE_TEX2D_SAMPLER(_OcclusionMap , _MainTex , SubUV).rgb , _OcclusionStrength);
		#endif

		float3 VL4Diff[4];
		       VL4Diff[0]   = LightingCalc(VLight0 , VLDiffuse.x , ShadeColor , ShadeMask);
		       VL4Diff[1]   = LightingCalc(VLight1 , VLDiffuse.y , ShadeColor , ShadeMask);
		       VL4Diff[2]   = LightingCalc(VLight2 , VLDiffuse.z , ShadeColor , ShadeMask);
		       VL4Diff[3]   = LightingCalc(VLight3 , VLDiffuse.w , ShadeColor , ShadeMask);
		float3 VLDiffColor  = saturate(VL4Diff[0] + VL4Diff[1] + VL4Diff[2] + VL4Diff[3]);

		       Lighting     = (DiffColor + SHDiffColor + VLDiffColor) * LightBoost;
	#endif
	#ifdef PASS_FA
		       Lighting     = DiffColor * LightBoost;
	#endif

	if (_LightLimitter) {
		float  MaxLight     = 1.0f;
		       MaxLight     = max(MaxLight , Lighting.r);
		       MaxLight     = max(MaxLight , Lighting.g);
		       MaxLight     = max(MaxLight , Lighting.b);
		       MaxLight     = min(MaxLight , 1.25f);
		       Lighting     = saturate(Lighting / MaxLight);
		       LightBase    = min(  LightBase , 2.5f);
		       VLightBase   = min( VLightBase , 2.5f);
		       SHLightBase  = min(SHLightBase , 2.5f);
	}

	float  LightPower = 0.0f;
	#ifdef PASS_FA
		       LightPower   = MonoColor(saturate(Lighting));
	#endif
