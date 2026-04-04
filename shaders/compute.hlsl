#include "common/common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter: register(u1);


[numthreads(256, 1, 1)] 
void main(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;
    if (i >= ParticleCount)
        return;
    Particle p = PrevParticles[i];


    if(p.Age < 0)
    {
        uint spawnIndex;
        InterlockedAdd(Emitter[0].ConsumedSpawns, 1, spawnIndex);
        if(spawnIndex < Emitter[0].SpawnCountThisFrame)
        {
            uint seed = i + FrameIndex * 12345;
            p.Position = 0;
            float angle = Random(seed * 3 + 1) * PI2;
            float speed = 0.6 + 0.01 * Random(seed * 3 + 1);
            p.Velocity = float3(Rotate(speed,angle), 0);
            p.Age = LifeTime * (0.9f + 0.2f * Random(i + FrameIndex * 12345));
        }
        NextParticles[i] = p;
    }
    else
    {
        float2 right = float2(p.Velocity.y, -p.Velocity.x);
        p.Velocity *= 1 + DeltaTime;
        p.Velocity += float3(right * 1 * DeltaTime, 0);
        p.Position = p.Position + p.Velocity * DeltaTime;
        p.Age -= DeltaTime;
        NextParticles[i] = p;
    }
}