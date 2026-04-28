#include "common.hlsli"


RWStructuredBuffer<EmitterData> Emitter : register(u1);

RWStructuredBuffer<DispatchArgs> Args : register(u4);

// Invariant after EmitterPSO:
// 0 <= AliveCount <= ParticleCount
// 0 <= SpawnCountThisFrame <= ParticleCount - AliveCount
// 0 <= AliveCount <= TotalCount <= ParticleCount
[numthreads(1,1,1)]
void main(uint3 tid : SV_DispatchThreadID)
{
    EmitterData data = Emitter[0];
    uint aliveCount = min(data.AliveCount, ParticleCount);

    uint increment = (uint)( (SpawnRate * DeltaTime ) * 65536.0) ;

    uint acc = data.SpawnAccumulator + increment;
    uint spawnCount = acc >> 16;

    acc &= 0xFFFF;

    uint freeSlots = ParticleCount - aliveCount;
    spawnCount = min(spawnCount, freeSlots);
    
    data.AliveCount = aliveCount;
    data.SpawnAccumulator = acc;
    data.SpawnCountThisFrame = spawnCount;
    data.ConsumedSpawns = 0;
    data.TotalCount = aliveCount + spawnCount;

    Args[0].ThreadGroupCountX = (data.TotalCount + 255) / 256;
    Args[0].ThreadGroupCountY = 1;
    Args[0].ThreadGroupCountZ = 1;

    Emitter[0] = data;
}