cbuffer Constants : register(b0)
{
    float4x4 ViewMatrix;
    float DeltaTime;
    uint FrameIndex;
    float LifeTime;
    uint ParticleCount;
    float3 Color;
    float SpawnRate;
    float2 SpawnPosition;
    float2 SpawnDistribution;
    float2 RemoveBox;
    float Size;
    float Velocity;
};
