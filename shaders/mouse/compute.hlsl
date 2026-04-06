#include "../common/common.hlsli"
#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

// Uses CustomData.xy to store unsnapped position
[numthreads(256, 1, 1)]
void main(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;
    if (i >= ParticleCount)
        return;

    Particle p = PrevParticles[i];

    float2 Prev = mousePosPrev;
    float2 Pos  = mousePos;

    if (p.Age < 0)
    {
        uint spawnIndex;
        InterlockedAdd(Emitter[0].ConsumedSpawns, 1, spawnIndex);

        if (spawnIndex < Emitter[0].SpawnCountThisFrame)
        {
            uint seed = i + FrameIndex * 12345;

            // random direction + random radius
            float angle = Random(seed) * PI2;
            float speed = 0.08 * Random(seed * 3 + 1);

            float2 rnd = Rotate(speed, angle);

            float trailT = (float)spawnIndex / max(1.0f, (float)Emitter[0].SpawnCountThisFrame);
            float2 loc = lerp(Prev, Pos, trailT);


            float mouseSpeed = length(Prev - Pos) / max(DeltaTime, 0.0001f)*0.2f + 0.001f;

            // random offset around the trail
            float offsetScale = min(1, max(4 / mouseSpeed, 0.2)) ;
            loc += rnd * offsetScale;

            p.CustomData.xy = loc;
            p.Position = SnapToGrid(p.CustomData.xy, GridSize);

            // initial visible age/lifetime
            p.Age = LifeTime * (0.9f + 0.1f * Random(seed * 17 + 5));
        }
    }
    else
    {
        // move using previous velocity
        p.CustomData.xy += p.Velocity * DeltaTime;

        // velocity points away from current mouse position
        float2 dir = p.CustomData.xy - Pos;

        float dirLen = length(dir);
        if (dirLen > 0.0001f)
            dir *= Velocity / dirLen;
        else
            dir = 0;

        p.Velocity = dir;

        p.Age -= DeltaTime;
    }

    p.Position = SnapToGrid(p.CustomData.xy, GridSize);

    if (p.Age < 0)
    {
        p.Size = 0;
        p.Color = 0;
    }
    else
    {
        // velocity points away from current mouse position
        float2 dir = p.CustomData.xy - Pos;

        float dirLen = saturate(0.2-length(dir))+0.05;

        float scaledAge = saturate(p.Age / LifeTime);
        p.Size = Size;
        p.Color = float4(Color, scaledAge * dirLen * 20);
    }

    NextParticles[i] = p;
}