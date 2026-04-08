#include "../common/commonCompute.hlsli"

cbuffer ParticleConstants : register(b0)
{
    float LifeTime;
    uint ParticleCount;
    float2 CenterPosition;
    float SpawnRate;
};
