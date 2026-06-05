#include "pbrBase.hlsli"

cbuffer SceneConstants : register(b0)
{
    float4x4 ViewProjection;
    LightDescription Lights[8];
	float3 CameraPos;
	int LightCount;
	float NormalScale;
}

cbuffer ModelConstants : register(b2)
{
    float4x4 WorldToModelTransform;
    float4x4 ModelTransform;
}


inline float3 ObjectToWorldNormal( in float3 norm ) {
	// Multiply by transposed inverse matrix,
	// actually using transpose() generates badly optimized code
	return normalize(
		WorldToModelTransform[0].xyz * norm.x +
		WorldToModelTransform[1].xyz * norm.y +
		WorldToModelTransform[2].xyz * norm.z
	);
}
float3 ObjectToWorldDir(float3 v)
{
    return normalize(mul(float4(v, 0.0), ModelTransform).xyz);
}
