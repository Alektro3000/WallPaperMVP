#include "../common/common.hlsli"
#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

float2 SnapToGrid(float2 value, float2 gridSize)
{
    return round(value / gridSize) * gridSize;
}

// Uses CustomData.xy to store unsnapped position
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

            uint stripId = WangHash(seed)%5;
            uint leftRight = WangHash(seed)%2 * 2 - 1;
            float2 rnd = Random2(seed)-0.5f;
            p.Position = Strips[stripId].Position + Strips[stripId].Size * rnd;
            p.Position.x = leftRight*p.Position.x;
            p.Velocity = p.Position;

            // initial visible age/lifetime
            p.Age = LifeTime * (0.9f + 0.1f * Random(seed * 17 + 5));
        }
    }
    else
    {
        // move using previous velocity
        p.Position += p.Velocity * DeltaTime;

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
        p.Color = float4(Color, scaledAge);
    }

    NextParticles[i] = p;
}