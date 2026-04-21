#include "common.hlsli"

struct GpuCounters
{
    uint AliveCount;
    uint Reserved0;
    uint Reserved1;
    uint Reserved2;
};

StructuredBuffer<Particle> SourceParticles : register(t0);
RWStructuredBuffer<Particle> DestParticles : register(u0);

StructuredBuffer<uint> ActiveList : register(t2);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

[numthreads(256, 1, 1)]
void main(uint3 tid : SV_DispatchThreadID)
{
    uint i = tid.x;
    if (i >= ParticleCount)
        return;

    Particle p = SourceParticles[i];

    if (p.Age < 0.0f)
        return;

    uint dst = ActiveList[i];

    if (dst < Emitter[0].AliveCount)
    {
        DestParticles[dst] = p;
    }
}