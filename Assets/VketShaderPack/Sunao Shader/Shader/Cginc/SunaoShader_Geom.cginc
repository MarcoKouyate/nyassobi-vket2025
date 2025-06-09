//--------------------------------------------------------------
//              Sunao Shader Geometry
//                      Copyright (c) 2022 揚茄子研究所
//--------------------------------------------------------------


[maxvertexcount(10)]
void geom(triangle VOUT IN[3] , inout TriangleStream<VOUT> GOUT) {

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN[0]);

	VOUT o;

	if (_FurEnable) {

		VOUT   Center[3];
		       Center[0]  = Fur_Interpolation(IN[0] , IN[1]);
		       Center[1]  = Fur_Interpolation(IN[1] , IN[2]);
		       Center[2]  = Fur_Interpolation(IN[2] , IN[0]);

		float3 FurLength  = GetScale(_FurLength , _FurFixScale);
		if (_FurFixScale) FurLength *= 10.0f;
		float  FurWidth   = lerp(10.0f , 0.25f , _FurWidth);

		float  FurDist[5];
		       FurDist[0] = 0.0f;
		       FurDist[1] = FurDist[0] + distance(Center[0].vertex.xyz , Center[1].vertex.xyz) * FurWidth;
		       FurDist[2] = FurDist[1] - distance(Center[1].vertex.xyz , Center[2].vertex.xyz) * FurWidth;
		       FurDist[3] = FurDist[2] + distance(Center[2].vertex.xyz , Center[0].vertex.xyz) * FurWidth;
		       FurDist[4] = FurDist[3] - distance(Center[0].vertex.xyz ,     IN[2].vertex.xyz) * FurWidth;

		GOUT.Append(Fur_GenerateRoot(Center[0]             , FurDist[0]));
		GOUT.Append(Fur_Generate    (Center[0] , FurLength , FurDist[0] , _FurRoughness , _FurGravity));
		GOUT.Append(Fur_GenerateRoot(Center[1]             , FurDist[1]));
		GOUT.Append(Fur_Generate    (Center[1] , FurLength , FurDist[1] , _FurRoughness , _FurGravity));
		GOUT.Append(Fur_GenerateRoot(Center[2]             , FurDist[2]));
		GOUT.Append(Fur_Generate    (Center[2] , FurLength , FurDist[2] , _FurRoughness , _FurGravity));
		GOUT.Append(Fur_GenerateRoot(Center[0]             , FurDist[3]));
		GOUT.Append(Fur_Generate    (Center[0] , FurLength , FurDist[3] , _FurRoughness , _FurGravity));
		GOUT.Append(Fur_GenerateRoot(    IN[2]             , FurDist[4]));
		GOUT.Append(Fur_Generate    (    IN[2] , FurLength , FurDist[4] , _FurRoughness , _FurGravity));

		GOUT.RestartStrip();

	}

}
