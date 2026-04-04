#ifndef COMMON_INCLUDED
#define COMMON_INCLUDED


cbuffer Constants : register(b0)
{
    float4x4 viewMatrix;
    float4 TintColor;
    uint ParticleCount;
    float DeltaTime;
    float2 Mouse;
    float LifeTime;
    float SpawnRate;
};

struct Particle
{
    float3 Position;
    float3 Velocity;
    float3 color;
    float age;
};

struct EmitterData{
     uint SpawnCountThisFrame;
     uint ConsumedSpawns;
     uint SpawnAccamulator;
};

float Random01(uint seed)
{
    seed ^= 2747636419u;
    seed *= 2654435769u;
    seed ^= seed >> 16;
    seed *= 2654435769u;
    seed ^= seed >> 16;
    seed *= 2654435769u;
    return (seed & 0x00FFFFFF) / 16777215.0;
}

#endif