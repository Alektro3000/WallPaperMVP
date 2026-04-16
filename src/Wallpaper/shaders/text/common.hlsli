#include "../common/commonCompute.hlsli"

cbuffer Constants : register(b0)
{
    uint ParticleCount;
    float3 padding;


    float LifeTime;
    float SpawnRate;
    float Size;
    float Speed;
    
    
    float InitRegion;
    float InitOffset;
};
