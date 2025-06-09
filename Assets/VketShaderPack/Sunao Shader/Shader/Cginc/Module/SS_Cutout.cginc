//-------------------------------------カットアウト
	#ifdef CUTOUT
		clip(OUT.a - _Cutout);
		if (_AlphaToMask) {
			OUT.a = saturate((OUT.a - _Cutout) * 10.0f);
		} else {
			OUT.a = 1.0f;
		}
	#endif
