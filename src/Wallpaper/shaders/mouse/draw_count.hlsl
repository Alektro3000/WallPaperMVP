#include "common.hlsli"

struct DispatchArgs
{
    uint ThreadGroupCountX;
    uint ThreadGroupCountY;
    uint ThreadGroupCountZ;
};

struct DrawIndexedArgs
{
    uint IndexCountPerInstance;
    uint InstanceCount;
    uint StartIndexLocation;
    int  BaseVertexLocation;
    uint StartInstanceLocation;
};

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
    uint aliveCount = 0;
    uint totalCount = min(data.AliveCount + data.SpawnCountThisFrame, ParticleCount);
    if (totalCount > 0)
    {
        uint lastId = totalCount - 1;
        aliveCount = ActiveList[lastId] + ((SparseParticles[lastId].Age >= 0) ? 1u : 0u);
    }
    data.AliveCount = min(aliveCount, ParticleCount);
    Emitter[0] = data;


    Args[0].ThreadGroupCountX = (aliveCount + 255) / 256;
    Args[0].ThreadGroupCountY = 1;
    Args[0].ThreadGroupCountZ = 1;
    
    DrawArgs[0].InstanceCount = data.AliveCount;

}