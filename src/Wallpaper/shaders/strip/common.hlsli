#include "../common/commonCompute.hlsli"

struct StripDescription
{
    float2 Position;
    float2 Size;
};

cbuffer StripConstants : register(b0)
{
    uint ParticleCount;
    float Size;
    float2 GridSize;

    StripDescription Strips[5];

    float3 Color;
    float SpawnRate;
    
    float Acceleration;
    float LifeTime;
};