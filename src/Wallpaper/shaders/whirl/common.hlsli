#include "../common/commonCompute.hlsli"

cbuffer ParticleConstants : register(b0)
{
    uint ParticleCount;
    float3 padding;

    float3 BeginColor;
    float SpawnRate;
    
    float3 EndColor;
    float LifeTime;

    float2 CenterPosition;

    float Speed;
    float Tangent;
    float Radial;
    float Size;
    
    float InitRegion;
    float InitOffset;
};
