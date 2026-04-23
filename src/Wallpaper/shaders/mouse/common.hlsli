#include "../common/commonCompute.hlsli"


cbuffer Constants : register(b0)
{
    float2 mousePos;
    float2 mousePosPrev;

    uint ParticleCount;
    float VelocityBlend;
    int2 _padding;
    
    float3 Color;
    float Size;
    
    float2 GridSize;
    float Velocity;
    float LifeTime;

    float SpawnRate;
    float SpawnRatePerUnit;
    float InitVelocity;
};