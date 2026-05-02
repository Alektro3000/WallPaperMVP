#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
StructuredBuffer<HashEntry> HashEntriesSrv : register(t4);
StructuredBuffer<CellRange> CellRangesSrv : register(t5);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

float2 Boundary(inout float2 position, float2 velocity)
{
    float2 limit = float2(ScreenRatio, 1.0f);
    if (position.x < -limit.x || position.x > limit.x)
    {
        velocity.x *= -0.45f;
        position.x = clamp(position.x, -limit.x, limit.x);
    }
    if (position.y < -limit.y || position.y > limit.y)
    {
        velocity.y *= -0.45f;
        position.y = clamp(position.y, -limit.y, limit.y);
    }
    return velocity;
}

[numthreads(256, 1, 1)] void main(uint3 dtid : SV_DispatchThreadID)
{
    uint sortedIndex = dtid.x;
    if (sortedIndex >= ParticleCount)
        return;

    EmitterData emitter = Emitter[0];
    if (sortedIndex >= emitter.TotalCount)
        return;

    HashEntry selfEntry = HashEntriesSrv[sortedIndex];
    uint i = selfEntry.ParticleIndex;
    Particle p = PrevParticles[i];

    if (p.Age < 0) // false && sortedIndex >= emitter.AliveCount)
    {
        uint seed = sortedIndex * 747796405u + FrameIndex * 2891336453u;
        
        p.Position = Random2(seed) * 2 - 1;
        p.Age = LifeTime;
    }

    float h = InfluenceRadius;
    int2 cell = CellCoord(p.Position);
    float density = 0.0f;
    float2 pressureForce = 0.0f;
    float2 viscosityForce = 0.0f;
    float2 separationForce = 0.0f;

    float minDist = SeparationRadius; // tune this

    [unroll] for (int oy = -1; oy <= 1; oy++)
    {
        [unroll] for (int ox = -1; ox <= 1; ox++)
        {
            int2 ncell = cell + int2(ox, oy);

            uint hash = CellHash(ncell);
            CellRange range = CellRangesSrv[hash];
            for (uint n = range.Start; n < range.End; n++)
            {
                HashEntry otherEntry = HashEntriesSrv[n];
                if (otherEntry.ParticleIndex == i)
                    continue;

                Particle other = PrevParticles[otherEntry.ParticleIndex];
                float2 delta = p.Position - other.Position;
                float dist = length(delta);
                float w = SphKernel(dist, h);
                density += w;


                if (dist < minDist)
                {
                    uint seed = (i * 747796405u) ^ FrameIndex;
                    float2 fallbackDir = Rotate(1.0f, Random(seed) * PI2);

                    float2 dir = dist > 0.00001f ? delta / dist : fallbackDir;

                    float q = 1.0f - dist / minDist;
                    separationForce += dir * q * q;
                }
                else
                {
                    float2 dir = delta / max(dist,0.0001f);
                    pressureForce += dir * SphKernel2(dist, h);
                viscosityForce += (other.Velocity - p.Velocity) * w;
                }
            }
        }
    }

    float pressure = max(0.0f, density - RestDensity) * Pressure;
    float2 acceleration = 
        pressureForce * pressure + 
        separationForce * SeparationStrength +
        viscosityForce * Viscosity + 
        float2(0.0f, Gravity);
    p.Velocity += acceleration * DeltaTime;
    //p.Velocity *= 0.999f;
    p.Velocity = Boundary(p.Position, p.Velocity);
    p.Position += p.Velocity * DeltaTime;
    p.CustomData1.x = density;
    p.Size = Size;
    p.Color = float4(BeginColor, 0.8f);

    p.Color.x = CellHashFromPosition(p.Position) == CellHashFromPosition(MousePos);

    if (p.Age >= 0.0f)
        InterlockedAdd(Emitter[0].AliveCountCheck, 1);

    NextParticles[i] = moveOutsideWindows(p);
}
