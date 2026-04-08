#include "../common/commonCompute.hlsli"

cbuffer Constants : register(b0)
{
    float LifeTime;
    uint ParticleCount;
    float SpawnRate;
};
