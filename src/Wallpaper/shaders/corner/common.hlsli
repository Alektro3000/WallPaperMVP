#include "../common/commonCompute.hlsli"
cbuffer Constants : register(b0)
{
    float3 Color;
    float SpawnRate;
    float2 SpawnPosition;
    float2 SpawnDistribution;
    float2 RemoveBox;
    float Size;
    float Velocity;
    float LifeTime;
    uint ParticleCount;
};
