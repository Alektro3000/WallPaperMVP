#include "common/common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

[numthreads(256, 1, 1)] void main(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;
    if (i >= ParticleCount)
        return;
    Particle p = PrevParticles[i];

    if (p.Age < 0)
    {
        uint spawnIndex;
        InterlockedAdd(Emitter[0].ConsumedSpawns, 1, spawnIndex);
        if (spawnIndex < Emitter[0].SpawnCountThisFrame)
        {
            uint seed = i + FrameIndex * 12345;
            float angle = Random(seed) * PI2;
            float speed = 0.3 + 0.3 * Random(seed * 3 + 1);

            p.Position = 0;
            p.Velocity = float3(Rotate(speed, angle), 0);
            p.Age = LifeTime * (0.5f + 0.5f * Random(seed+2));
        }
    }
    float scaledAge = p.Age/LifeTime;

    float2 right = p.Velocity.yx * float2(1, -1) - p.Position.xy * 0.5f;
    p.Velocity += float3(right * 1 * DeltaTime, 0);

    p.Position = p.Position + p.Velocity * DeltaTime;
    p.Age -= DeltaTime;

    p.Size = 0.06 + 0.05 * scaledAge * scaledAge;
    p.Color = float4(0.2f, 0.9f, 1.f, scaledAge);
    p.Color.g *= 1.2f - scaledAge;
    //p.Color.r *= 0.2f + scaledAge;
    
    NextParticles[i] = p;
}