#include "../common/commonCompute.hlsli"

cbuffer Constants : register(b0)
{
    uint ParticleCount;
    float3 padding;

    float3 BeginColor;
    float LifeTime;
    
    float3 EndColor;
    float SpawnRate;

    float Size;
    float Speed;
    float InitRegion;
    float InitOffset;
};
