#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
StructuredBuffer<HashEntry> HashEntriesSrv : register(t4);
StructuredBuffer<CellRange> CellRangesSrv : register(t5);
RWStructuredBuffer<Particle> NextParticles : register(u0);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

float2 Boundary(inout float2 position, float2 velocity, float2 limit)
{
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
        p.Age = 1;
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

    float2 limits = float2(ScreenRatio, 1.0f);
    float2 bounds = limits * SoftBoundaryScale - 1/BoundaryHardness;

    float2 leftBottom = saturate((-bounds - p.Position) * BoundaryHardness);
    float2 rightTop   = saturate(( p.Position - bounds) * BoundaryHardness);

    float2 boundaryForce = leftBottom - rightTop;

    float pressure = max(0.0f, density - RestDensity) * Pressure;
    float2 acceleration = 
        pressureForce * pressure + 
        separationForce * SeparationStrength +
        boundaryForce * BoundaryForce +
        viscosityForce * Viscosity + 
        getMoveOutWindowForce(p.Position, WindowsOffset) * WindowsForce + 
        float2(0.0f, Gravity);

    if ((MouseButtons & 3u) != 0u)
    {
        float2 toMouse = MousePos - p.Position;
        float distToMouse = length(toMouse);
        float radius = max(MouseRadius, 0.0001f);
        if (distToMouse < radius)
        {
            float2 dirToMouse = toMouse / max(distToMouse, 0.0001f);
            float t = 1.0f - distToMouse / radius;
            float falloff = t * t;
            acceleration += ((MouseButtons & 1u) ? dirToMouse : -dirToMouse) * (MouseStrength * falloff);
        }
    }

    p.Velocity += acceleration * SmoothedDeltaTime;
    p.Velocity += getParticleFieldVelocity(p.Position) * WindowsVelocity;
    
    //p.Velocity *= 0.999f;
    p.Position += p.Velocity * SmoothedDeltaTime;
    p.Velocity = Boundary(p.Position, p.Velocity, limits);
    p.CustomData1.x = density;
    p.Size = Size;
    p.Color = float4(Color, 0.8f);

    //p.Color.x = CellHashFromPosition(p.Position) == CellHashFromPosition(MousePos);

    if (p.Age >= 0.0f)
        InterlockedAdd(Emitter[0].AliveCountCheck, 1);

    NextParticles[i] = p;
}
