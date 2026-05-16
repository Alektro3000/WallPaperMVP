#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);

RWStructuredBuffer<EmitterData> Emitter : register(u1);

float2 EvalCubic(float t)
{
    return ((CatmulA * t + CatmulB) * t + CatmulC) * t + CatmulD;
}

float2 EvalCubicTangent(float t)
{
    return (3.0f * CatmulA * t + 2.0f * CatmulB) * t + CatmulC;
}

float map01(float x, float minValue, float maxValue)
{
    return saturate((x - minValue) / (maxValue - minValue));
}

// Uses CustomData.xy to store unsnapped position
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

    float MouseSpeedLerp = map01(MouseSpeed, StationaryLerpStart, StationaryLerpEnd);
    float OffsetScale = map01(MouseSpeed, OffsetLerpStart, OffsetLerpEnd);
    
    if (i >= aliveCount)
    {
        uint spawnIndex = i - aliveCount;

        uint seed = spawnIndex + FrameIndex * 12345;

        float trailT = (float)spawnIndex / max(1.0f, emitter.SpawnCountThisFrame);
        
        float2 loc = EvalCubic(trailT);

        float2 RndUnit = Rotate(1 + 0.1 * Random(seed * 3 + 1), Random(seed * 131 + 3) * PI2);

        float2 vel = EvalCubicTangent(trailT);
        float2 velDir = MouseFrameDistance > 0.0001f ? normalize(vel) : 0;
        float2 normal = float2(-velDir.y, velDir.x);

        // random offset around the trail
        float phase = PhaseShift + trailT * WaveCyclesOnSegment;
        float offsetSize = StripWidth * OffsetScale;
        float2 trailOffset = (WangHash(seed * 3 + 1) % 2 ? -1 : 1) * sin(phase) * normal * offsetSize;
        float2 stationaryOffset = RndUnit * StationaryOffset;
        loc += lerp(stationaryOffset, trailOffset, MouseSpeedLerp);

        p.CustomData.xy = loc;
        p.Position = SnapToGrid(loc, GridSize);

        bool isSpark = Random(seed * 3 + 1) < SparkPercent;

        p.CustomData1.x = isSpark;
        p.CustomData1.y = (WangHash(seed * 5  + 3) % 2 ) ? 1 : -1;

        float VelSize = InitVelocity * (1 - isSpark);
        float2 trailVelocity = velDir * VelSize;

        float2 stationaryVelocity = RndUnit * StationaryVelocity * (1-MouseSpeedLerp);
        p.Velocity = lerp(stationaryVelocity, trailVelocity, MouseSpeedLerp);
        
        // initial visible age/lifetime
        p.Age = LifeTime * (0.9f - isSpark * 0.4 + 0.1f * Random(seed));
    }
    else
    {
        // move using previous velocity
        p.CustomData.xy += p.Velocity * DeltaTime * (1 + p.Age * p.CustomData1.x * 10);

        // blend instead of overwrite
        p.Velocity *= VelocityBlend;

        // integrate
        p.Age -= DeltaTime;
    }

    p.Position = SnapToGrid(p.CustomData.xy, GridSize);

    // velocity points away from current mouse position
    float scaledAge = saturate(p.Age / LifeTime);
    p.Size = (p.Age >= 0) * Size; 
    p.Color = float4(lerp(EndColor, BeginColor, scaledAge), (scaledAge + 0.3 * p.CustomData1.x) );
    
    NextParticles[i] = p;
}