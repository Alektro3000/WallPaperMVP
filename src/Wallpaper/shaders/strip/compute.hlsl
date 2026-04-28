#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

// Uses CustomData.xy to store unsnapped position
[numthreads(256, 1, 1)] void main(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;
    if (i >= ParticleCount)
        return;

    EmitterData emitter = Emitter[0];
    uint totalCount = emitter.TotalCount;
    uint aliveCount = emitter.AliveCount;

    if (i >= totalCount)
        return;

    Particle p = PrevParticles[i];

    if (i >= aliveCount)
    {
        uint seed = i + FrameIndex * 12345;

        uint stripId = WangHash(seed) % 5;
        int leftRight = (WangHash(seed + 1) % 2) * 2 - 1;
        float2 rnd = Random2(seed) - 0.5f;
        p.CustomData = Strips[stripId].Position + Strips[stripId].Size * rnd;
        p.CustomData.x = leftRight * p.CustomData.x;
        p.Velocity = p.CustomData * Acceleration;

        // initial visible age/lifetime
        p.Age = LifeTime * (0.9f + 0.1f * Random(seed * 17 + 5));
    }
    else
    {
        // move using previous velocity
        p.CustomData += p.Velocity * DeltaTime;

        p.Age -= DeltaTime;
    }

    p.Position = SnapToGrid(p.CustomData, GridSize);

    if (p.Age < 0)
    {
        p.Size = 0;
        p.Color = 0;
    }
    else
    {
        // velocity points away from current mouse position
        float scaledAge = saturate(p.Age / LifeTime);
        float initRegion = saturate(1.2f - scaledAge * 10);
        p.Size = Size;
        p.Color = float4(Color, scaledAge - initRegion);
    }

    NextParticles[i] = updateParticleField(p);
}