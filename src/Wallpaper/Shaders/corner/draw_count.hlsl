#include "common.hlsli"

StructuredBuffer<uint> ActiveList : register(t2);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

RWStructuredBuffer<DispatchArgs> Args : register(u4);
RWStructuredBuffer<DrawIndexedArgs> DrawArgs : register(u5);

StructuredBuffer<Particle> SparseParticles : register(t0);

[numthreads(1, 1, 1)]
void main(uint3 tid : SV_DispatchThreadID)
{
    if (tid.x != 0)
        return;
    
    // Update emitter count
    EmitterData data = Emitter[0];
    uint aliveCount = 0;
    uint totalCount = data.TotalCount;
    if (totalCount > 0)
    {
        uint lastId = totalCount - 1;
        aliveCount = ActiveList[lastId] + ((SparseParticles[lastId].Age >= 0) ? 1u : 0u);
    }
    data.AliveCount = min(aliveCount, ParticleCount);
    Emitter[0] = data;

    DrawArgs[0].InstanceCount = aliveCount;

}