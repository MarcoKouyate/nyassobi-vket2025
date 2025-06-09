//-------------------------------------ファー
	float  FurWidth    = lerp(10.0f , 0.25f , _FurWidth);

	float  FurShape    = 0.0f;
	if (_FurShapeMode == 0) {
		   FurShape   += frac(dot(MainUV * FurWidth * 0.3f , float2(39.0f , 75.0f)));
		   FurShape   += frac(dot(MainUV * FurWidth * 0.3f , float2(63.0f , 51.0f)));
		   FurShape    = saturate(FurShape * (1.0f - IN.furuv.y));
		   FurShape   *= saturate((FurShape * FurShape - 0.01f) * 15.0f);
	}
	if (_FurShapeMode == 1) {
		   FurShape   += sin(dot(MainUV * FurWidth * 2.0f , float2(39.0f , 75.0f))) + 1.0f;
		   FurShape   += cos(dot(MainUV * FurWidth * 2.0f , float2(63.0f , 51.0f))) + 1.0f;
		   FurShape    = saturate(FurShape * (1.0f - IN.furuv.y));
		   FurShape   *= saturate((FurShape * FurShape - 0.05f) *  5.0f);
	}

	if (_FurShapeMode == 8) {
		   FurShape    = MonoColor(tex2D(_FurShapeTex , frac(MainUV * FurWidth)).rgb);
		   FurShape    = saturate(FurShape * (1.0f - IN.furuv.y));
		   FurShape   *= saturate((FurShape * FurShape - 0.05f) * 10.0f);
	}

	       Color *= _FurColor.rgb;
	       OUT.a *= FurShape * 0.6f * _FurColor.a;
