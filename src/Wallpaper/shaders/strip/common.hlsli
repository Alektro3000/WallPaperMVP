#include "../common/commonCompute.hlsli"

struct StripDescription
{
    float2 Position;
    float2 Size;
};

cbuffer StripConstants : register(b0)
{
    float LifeTime;
    uint ParticleCount;
    float2 GridSize;

    StripDescription Strips[5];

    float3 Color;
    float SpawnRate;
    
    float Acceleration;
    float Size;
};