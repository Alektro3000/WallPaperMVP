#include "common/common.hlsli"


RWStructuredBuffer<EmitterData> Emitter : register(u1);

[numthreads(1,1,1)]
void CS_EmitSetup(uint3 tid : SV_DispatchThreadID)
{
    uint increment = (uint)(SpawnRate * DeltaTime * 65536.0);

    uint acc =  Emitter[0].SpawnAccamulator + increment;
    uint spawnCount = acc >> 16;
    acc &= 0xFFFF;

    Emitter[0].SpawnAccamulator = acc;
    Emitter[0].SpawnCountThisFrame = spawnCount;
    Emitter[0].ConsumedSpawns = 0;
}
