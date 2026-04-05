cbuffer Constants : register(b0)
{
    float4x4 ViewMatrix;
    float DeltaTime;
    uint FrameIndex;
    float LifeTime;
    uint ParticleCount;
    float SpawnRate;
};
