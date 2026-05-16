#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

[numthreads(256, 1, 1)] void MAIN_CS(uint3 dtid : SV_DispatchThreadID)
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
        float angle = Random(seed) * PI2;
        float speed = Speed + Speed * Random(seed * 3 + 1);

        p.Position = CenterPosition;
        p.Velocity = Rotate(speed, angle);
        p.Age = LifeTime * (0.9f + 0.1f * Random(seed + 2));
    }
    else
    {

        float2 tangent = float2(p.Velocity.y, -p.Velocity.x) * Tangent;
        float2 radial = (p.Position.xy - CenterPosition) * Radial;
        p.Velocity += (tangent + radial) * DeltaTime;

        p.Position = p.Position + p.Velocity * DeltaTime;
        p.Age -= DeltaTime;
    }
    
    float scaledAge = p.Age / LifeTime;

    if (p.Age < 0)
    {
        p.Size = 0;
        p.Color = 0;
    }
    else
    {
        float initRegion = saturate((scaledAge - InitOffset) * InitRegion);
        p.Size = Size * (1 - initRegion);
        p.Color = float4(lerp(EndColor, BeginColor, scaledAge), scaledAge);
    }

    NextParticles[i] = updateParticleField(p);
}