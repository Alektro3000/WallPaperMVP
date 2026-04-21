#include "common.hlsli"

RWStructuredBuffer<uint> AliveList: register(u2);

StructuredBuffer<Particle> SparseParticles : register(t0);

[numthreads(256, 1, 1)] 
void main(uint3 dtid : SV_DispatchThreadID)
{
    uint id = dtid.x;
    if(id < ParticleCount)
        AliveList[id] = SparseParticles[id].Age >= 0 ? 1 : 0;
}