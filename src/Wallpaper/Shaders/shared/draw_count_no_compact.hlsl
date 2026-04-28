#include "../common/commonCompute.hlsli"

StructuredBuffer<uint> ActiveList : register(t2);
RWStructuredBuffer<EmitterData> Emitter : register(u1);

RWStructuredBuffer<DispatchArgs> Args : register(u5);
RWStructuredBuffer<DrawIndexedArgs> DrawArgs : register(u6);

StructuredBuffer<Particle> SparseParticles : register(t0);

[numthreads(1, 1, 1)]
void main(uint3 tid : SV_DispatchThreadID)
{
    if (tid.x != 0)
        return;
    
    // Update emitter count
    EmitterData data = Emitter[0];
    uint aliveCount = data.TotalCount;
    data.AliveCount = aliveCount;
    Emitter[0] = data;

    DrawArgs[0].InstanceCount = aliveCount;

}