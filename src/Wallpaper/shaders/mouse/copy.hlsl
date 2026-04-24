#include "common.hlsli"

StructuredBuffer<Particle> SourceParticles : register(t0);
RWStructuredBuffer<Particle> DestParticles : register(u0);

StructuredBuffer<uint> ActiveList : register(t2);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

// At this point Emitter.AliveCount is still previous-frame count.
// Current compacted destination index is defined only by:
//  - particle alive test
//  - ActiveList[i] exclusive prefix
[numthreads(256, 1, 1)] void main(uint3 tid : SV_DispatchThreadID)
{
    uint i = tid.x;
    if (i >= Emitter[0].TotalCount)
        return;

    Particle p = SourceParticles[i];

    if (p.Age < 0.0f)
        return;

    DestParticles[ActiveList[i]] = p;
}