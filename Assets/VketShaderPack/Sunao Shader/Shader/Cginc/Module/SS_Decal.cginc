//----デカール
	float4 DecalColor   = float4(0.0f , 0.0f , 0.0f , 1.0f);

	if (_DecalEnable) {

		float2   DecalUV       = (float2)0.0f;
		float2x2 DecalRot      = float2x2(IN.decal.z, -IN.decal.w, IN.decal.w, IN.decal.z);

		if  (_DecalMirror <  4) {
			DecalUV    = IN.uv   - float2(_DecalPosX , _DecalPosY) + IN.decal2.zw;
		} else {
			DecalUV.x  = 0.5f + (floor(_DecalPosX + 0.5f) - 0.5f) * abs(2.0f * IN.uv.x -1.0f);
			DecalUV.y  = IN.uv.y;
			DecalUV    = DecalUV - float2(_DecalPosX , _DecalPosY) + IN.decal2.zw;
		}

		if ((_DecalMirror == 1) | (_DecalMirror == 3)) {
			DecalUV    = lerp(DecalUV , float2(-DecalUV.x , DecalUV.y) , saturate(IN.tangent.w));
			DecalUV.x += IN.decal2.z * saturate(IN.tangent.w) * 2.0f;
		}
		if  (_DecalMirror == 5) {
			DecalUV    = lerp(DecalUV , float2(-DecalUV.x , DecalUV.y) , floor(IN.uv.x + 0.5f));
			DecalUV.x += IN.decal2.z * floor(IN.uv.x + 0.5f) * 2.0f;
		}

		         DecalUV       = mul(DecalRot, DecalUV - IN.decal2.zw) + IN.decal2.zw;
		         DecalUV      *= IN.decal.xy;

		float2   DecalScrUV    = (DecalUV + IN.decanm.xy) * IN.decanm.zw;
		         DecalScrUV   += float2(_DecalScrollX , _DecalScrollY) * _Time.y;

		         DecalColor    = tex2Dbias(_DecalTex  , float4(DecalScrUV , 0.0f , IN.decal2.x * IN.decal2.y - 1.0f)) * _DecalColor;
		         DecalColor.a *= saturate((0.5f - abs(DecalUV.x - 0.5f)) * 1000.0f) * saturate((0.5f - abs(DecalUV.y - 0.5f)) * 1000.0f);

		if (_DecalMirror == 2) DecalColor.a = DecalColor.a * (1.0f - saturate(IN.tangent.w));
		if (_DecalMirror == 3) DecalColor.a = DecalColor.a *         saturate(IN.tangent.w);

		#ifdef TRANSPARENT
			if ((_DecalMode == 0) | (_DecalMode == 5)) {
				Color        = lerp(Color , lerp(DecalColor.rgb , Color , OUT.a) , DecalColor.a);
			}
			if  (_DecalMode == 1) {
				Color        = lerp(Color ,                       Color * OUT.a  , DecalColor.a);
				DecalColor.a = MonoColor(DecalColor.rgb) * DecalColor.a;
			}
			if ((_DecalMode == 2) | (_DecalMode == 3)) {
				DecalColor.a = DecalColor.a * OUT.a;
			}
			if  (_DecalMode == 4) {
				DecalColor.a = MonoColor(DecalColor.rgb) * DecalColor.a * _DecalEmission;
			}
		#endif

		         OUT.a         = max(OUT.a , DecalColor.a);

		if ((_DecalMode == 0) | (_DecalMode == 5)) {
			Color = lerp(Color ,          DecalColor.rgb , DecalColor.a);
		}
		if  (_DecalMode == 1) {
			Color = saturate(    Color  + DecalColor.rgb * DecalColor.a);
		}
		if  (_DecalMode == 2) {
			Color = lerp(Color , Color  * DecalColor.rgb , DecalColor.a);
		}
		if  (_DecalMode == 3) {
			float DecalMixCol = saturate(max(Color.r , max(Color.g , Color.b)) + _DecalBright);
			Color = lerp(Color , DecalMixCol * DecalColor.rgb , DecalColor.a);
		}
		if ((_DecalMode == 4) | (_DecalMode == 5)) {
			DecalColor.rgb = DecalColor.rgb * DecalColor.a;
		}

	}
