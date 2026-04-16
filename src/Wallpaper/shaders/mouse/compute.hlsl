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
    float2 Pos = mousePos;

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

            float mouseSpeed = length(Prev - Pos) / max(DeltaTime, 0.0001f) * 0.2f + 0.001f;

            // random offset around the trail
            float offsetScale = min(1, max(4 / mouseSpeed, 0.2));
            loc += rnd * offsetScale;

            p.CustomData.xy = loc;
            p.Position = SnapToGrid(p.CustomData.xy, GridSize);

            float r = Random(seed * 13 + 9);
            bool isSpark = r > 0.9; // ~10% sparks

            p.CustomData1.x = isSpark;

            float2 VelDir = (Prev - Pos) / mouseSpeed;
            float VelSpeedScale = min(1, max(4 / mouseSpeed, 0.2));
            float VelSize = InitVelocity / VelSpeedScale * (1-isSpark);
            p.Velocity = VelSize * VelDir;
            // initial visible age/lifetime
            p.Age = LifeTime * (0.9f - isSpark * 0.4 + 0.1f * Random(seed * 17 + 5));
        }
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
        float swirlSign = (WangHash(i ^ FrameIndex) & 1) ? 1.0f : -1.0f;

        // stronger near cursor, weaker far away
        float nearMouse = saturate(1.0f - dist / 0.35f);
        float nearMouseRadial = saturate(1.0f - dist / 0.55f);
        float age01 = saturate(1.0f - p.Age / LifeTime);

        // motion of mouse itself
        float2 mouseDelta = Pos - Prev;
        float mouseDeltaLen = length(mouseDelta);
        float2 mouseDir = mouseDeltaLen > 0.0001f ? mouseDelta / mouseDeltaLen : 0;

        // desired velocity
        float2 desiredVel =
            radial * 5 * nearMouseRadial + 
            tangent * 0.9f * swirlSign * (nearMouse + p.CustomData1.x * 10) + 
            mouseDir * 0.5f * (1-p.CustomData1.x);
        // blend instead of overwrite
        p.Velocity = lerp(desiredVel * Velocity, p.Velocity, Emitter[0].VelocityBlend);

        // integrate
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

        float dirLen = saturate(0.2 - length(dir)) + 0.05;

        float scaledAge = saturate(p.Age / LifeTime);
        p.Size = Size;
        p.Color = float4(Emitter[0].VelocityBlend.xxx, (scaledAge + 0.3 * p.CustomData1.x) * dirLen * 20);
    }

    NextParticles[i] = p;
}