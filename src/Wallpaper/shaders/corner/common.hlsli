#include "../common/commonCompute.hlsli"
cbuffer Constants : register(b0)
{
    uint ParticleCount;
    int3 _padding;

    float3 Color;
    float SpawnRate;

    float2 SpawnPosition;
    float2 SpawnDistribution;

    float2 RemoveBox;
    float Size;
    float LifeTime;
    
    float Velocity;
};
