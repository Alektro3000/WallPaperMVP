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
            float angle = Random(seed) * PI2;
            float speed = 0.2 + 0.2 * Random(seed * 3 + 1);

            p.Position = CenterPosition;
            p.Velocity = Rotate(speed, angle);
            p.Age = LifeTime * (0.9f + 0.1f * Random(seed+2));
        }
    }
    float scaledAge = p.Age/LifeTime;

    float2 right = p.Velocity.yx * float2(1, -1) - (p.Position.xy - CenterPosition) * 0.1f;
    p.Velocity += right * DeltaTime;

    p.Position = p.Position + p.Velocity * DeltaTime;
    p.Age -= DeltaTime;

    p.Size = 0.06 + 0.05 * scaledAge * scaledAge;
    p.Color = float4(0.2f, 0.9f *  (1.2f - scaledAge), 1.f, scaledAge);
    
    if (p.Age < 0)
    {
        p.Size = 0;
        p.Color = 0;
    }
    else
    {
        float initRegion = saturate(scaledAge * 3 - 1.3f);
        p.Size = 0.06 - initRegion * 0.06f;
        p.Color = float4(0.4f , 0.9f * (1.2f - scaledAge), 1.f, scaledAge);
    }

    NextParticles[i] = updateParticleField(p);
}