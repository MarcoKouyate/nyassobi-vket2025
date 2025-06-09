//-------------------------------------出力オプション
//----ガンマ修正
	if (_EnableGammaFix) {
		_GammaR = max(_GammaR , 0.00001f);
		_GammaG = max(_GammaG , 0.00001f);
		_GammaB = max(_GammaB , 0.00001f);

	       OUT.r    = pow(OUT.r , 1.0f / (1.0f / _GammaR));
	       OUT.g    = pow(OUT.g , 1.0f / (1.0f / _GammaG));
	       OUT.b    = pow(OUT.b , 1.0f / (1.0f / _GammaB));
	}

//----明度修正
	if (_EnableBlightFix) {
	       OUT.rgb *= _BlightOutput;
	       OUT.rgb  = max(OUT.rgb + _BlightOffset , 0.0f);
	}

//----出力リミッタ
	if (_LimitterEnable) {
	       OUT.rgb  = min(OUT.rgb , _LimitterMax);
	}

//----BlendOpの代用
	#ifdef PASS_FA
		float OutAlpha = saturate(MonoColor(OUT.rgb));
		#ifndef TRANSPARENT
	       OUT.a    = OutAlpha;
		#endif
		#ifdef TRANSPARENT
	       OUT.rgb *= OUT.a;
	       OUT.a    = OutAlpha * pow(OUT.a , 1.8f);
		#endif
	#endif
//-------------------------------------フォグ
	UNITY_APPLY_FOG(IN.fogCoord, OUT);
