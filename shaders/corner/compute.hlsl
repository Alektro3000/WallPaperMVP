#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

[numthreads(256, 1, 1)]
void main(uint3 dtid : SV_DispatchThreadID)
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

            float2 rnd = normalize(Random2(seed)-0.5f);

            p.Position = sign(rnd) * (SpawnPosition-SpawnDistribution) + rnd * SpawnDistribution;
            p.Velocity = rnd * (0.03f);

            // initial visible age/lifetime
            p.Age = LifeTime;
        }
    }
    else
    {
        //p.Velocity += p.Position * DeltaTime * 0.01f;
        p.Position += p.Velocity * DeltaTime;

        if(any(abs(p.Position) > RemoveBox))
            p.Age -= DeltaTime * 6;
        p.Age -= DeltaTime;
    }

    if (p.Age < 0)
    {
        p.Size = 0;
        p.Color = 0;
    }
    else
    {
        // velocity points away from current mouse position
        float scaledAge = saturate(p.Age / LifeTime);
        p.Size = Size;

        p.Color = float4(saturate(Color), scaledAge > 0.8f ? 5-5*scaledAge : scaledAge*1.25);
    }

    NextParticles[i] = updateParticleField(p);
}