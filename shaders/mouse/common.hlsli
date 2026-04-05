cbuffer Constants : register(b0)
{
    float4x4 ViewMatrix;
    float DeltaTime;
    uint FrameIndex;
    float LifeTime;
    uint ParticleCount;
    float2 mousePos;
    float2 mousePosPrev;
    float SpawnRate;
    float SpawnRatePerUnit;
    float2 GridSize;
    float3 Color;
    float Size;
    float Velocity;
};
