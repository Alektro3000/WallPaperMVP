#include "common.hlsli"

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
            float speed = Speed + Speed * Random(seed * 3 + 1);

            p.Position = CenterPosition;
            p.Velocity = Rotate(speed, angle);
            p.Age = LifeTime * (0.9f + 0.1f * Random(seed + 2));
        }
    }
    float scaledAge = p.Age / LifeTime;

    float2 tangent = float2(p.Velocity.y, -p.Velocity.x) * Tangent;
    float2 radial = (p.Position.xy - CenterPosition) * Radial;
    p.Velocity += (tangent + radial) * DeltaTime;

    p.Position = p.Position + p.Velocity * DeltaTime;
    p.Age -= DeltaTime;

    if (p.Age < 0)
    {
        p.Size = 0;
        p.Color = 0;
    }
    else
    {
        float initRegion = saturate((scaledAge - InitOffset) * InitRegion);
        p.Size = Size * (1 - initRegion);
        p.Color = float4(0.4f, 0.9f * (1.2f - scaledAge), 1.f, scaledAge);
    }

    NextParticles[i] = updateParticleField(p);
}