#include "../common/commonCompute.hlsli"

cbuffer ParticleConstants : register(b0)
{
    uint ParticleCount;
    float3 padding;

    float2 CenterPosition;
    float LifeTime;
    float SpawnRate;

    float Speed;
    float Tangent;
    float Radial;
    float Size;
    
    float InitRegion;
    float InitOffset;
};
