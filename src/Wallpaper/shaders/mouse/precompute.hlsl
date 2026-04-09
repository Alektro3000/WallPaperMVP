#include "common.hlsli"


RWStructuredBuffer<EmitterData> Emitter : register(u1);

[numthreads(1,1,1)]
void main(uint3 tid : SV_DispatchThreadID)
{
    EmitterData data = Emitter[0];
    float dist = length(mousePos - mousePosPrev);

    uint increment = (uint)( (SpawnRate * DeltaTime + SpawnRatePerUnit * dist ) * 65536.0) ;

    uint acc = data.SpawnAccumulator + increment;
    uint spawnCount = acc >> 16;
    if(data.ConsumedSpawns < data.SpawnCountThisFrame)
        spawnCount += data.SpawnCountThisFrame - data.ConsumedSpawns;
    acc &= 0xFFFF;

    Emitter[0].SpawnAccumulator = acc;
    Emitter[0].SpawnCountThisFrame = spawnCount;
    Emitter[0].ConsumedSpawns = 0;
}
