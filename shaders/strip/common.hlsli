struct StripDescription
{
    float2 Position;
    float2 Size;
};

cbuffer StripConstants : register(b0)
{
    float4x4 ViewMatrix;

    float DeltaTime;
    uint FrameIndex;
    float LifeTime;
    uint ParticleCount;

    StripDescription Strips[5];

    float3 Color;

    float SpawnRate;
    float2 GridSize;
    float Acceleration;
    float Size;
};