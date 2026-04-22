#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);

RWStructuredBuffer<EmitterData> Emitter : register(u1);
StructuredBuffer<GpuMouseBuffer> Counters : register(t4);

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

    float2 Prev = mousePosPrev;
    float2 Pos = mousePos;

    Particle p = PrevParticles[i];

    if (i >= aliveCount)
    {
        uint spawnIndex = i - aliveCount;

        uint seed = spawnIndex + FrameIndex * 12345;

        // random direction + random radius
        float angle = Random(seed) * PI2;
        float speed = 0.08 * Random(seed * 3 + 1);

        float2 rnd = Rotate(speed, angle);

        float trailT = (float)spawnIndex / max(1.0f, emitter.SpawnCountThisFrame);
        float2 loc = lerp(Prev, Pos, trailT);

        float mouseSpeed = length(Prev - Pos) / max(DeltaTime, 0.0001f) * 0.2f + 0.001f;

        // random offset around the trail
        float offsetScale = min(1, max(4 / mouseSpeed, 0.2));
        loc += rnd * offsetScale;

        p.CustomData.xy = loc;
        p.Position = SnapToGrid(loc, GridSize);

        float r = Random(seed * 13 + 9);
        bool isSpark = r > 0.9; // ~10% sparks

        p.CustomData1.x = isSpark;
        p.CustomData1.y = (Random(seed * 19u + 7u) > 0.5f) ? 1.0f : -1.0f;

        float2 VelDir = (Prev - Pos) / mouseSpeed;
        float VelSpeedScale = min(1, max(4 / mouseSpeed, 0.2));
        float VelSize = InitVelocity / VelSpeedScale * (1 - isSpark);
        p.Velocity = VelSize * VelDir;
        // initial visible age/lifetime
        p.Age = LifeTime * (0.9f - isSpark * 0.4 + 0.1f * Random(seed * 17 + 5));
        p.Color = float4(0, 1, 0, 1);
    }
    else
    {
        // move using previous velocity
        p.CustomData.xy += p.Velocity * DeltaTime * (1 + p.Age * p.CustomData1.x * 10);

        float2 toParticle = p.CustomData.xy - Pos;
        float dist = length(toParticle);
        float2 radial = 0;
        if (dist > 0.0001f)
            radial = toParticle / dist;

        // perpendicular to radial
        float2 tangent = float2(-radial.y, radial.x);

        // choose random swirl direction per particle
        float swirlSign = p.CustomData1.y;

        // stronger near cursor, weaker far away
        float nearMouse = saturate(1.0f - dist / 0.35f);
        float nearMouseRadial = saturate(1.0f - dist / 0.55f);
        float age01 = saturate(1.0f - p.Age / LifeTime);

        // motion of mouse itself
        float2 mouseDelta = Pos - Prev;
        float mouseDeltaLen = length(mouseDelta);
        float2 mouseDir = mouseDeltaLen > 0.0001f ? mouseDelta / mouseDeltaLen : 0;

        float mouseSpeed = mouseDeltaLen / max(DeltaTime, 0.0001f);
        float swirlStrength = saturate(mouseSpeed * 20) * nearMouse;

        // desired velocity
        float2 desiredVel =
            radial * -0.35 * nearMouseRadial +
            tangent * 1.8f * swirlSign * swirlStrength * (nearMouse + p.CustomData1.x * 10) +
            mouseDir * 0.5f * (1 - p.CustomData1.x);

        // blend instead of overwrite
        p.Velocity += desiredVel * Velocity * DeltaTime;
        p.Velocity *= Counters[0].VelocityBlend;

        // integrate
        p.Age -= DeltaTime;
        p.Color = float4(p.Age,1,1,1);
    }

    p.Position = SnapToGrid(p.CustomData.xy, GridSize);

    // velocity points away from current mouse position
    float2 dir = p.CustomData.xy - Pos;

    float dirLen = saturate(0.2 - length(dir)) + 0.05;

    float scaledAge = saturate(p.Age / LifeTime);
    p.Size = Size; 
    //p.Color = float4(Color, (scaledAge + 0.3 * p.CustomData1.x) * dirLen * 20);

    NextParticles[i] = p;
}