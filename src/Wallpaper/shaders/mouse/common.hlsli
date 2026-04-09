#include "../common/commonCompute.hlsli"

cbuffer Constants : register(b0)
{
    float LifeTime;
    uint ParticleCount;
    float2 mousePos;

    float2 mousePosPrev;
    float SpawnRate;
    float SpawnRatePerUnit;
    
    float3 Color;
    float Size;
    
    float2 GridSize;
    float Velocity;
};
