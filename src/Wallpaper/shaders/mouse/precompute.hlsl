#include "common.hlsli"

struct DispatchArgs
{
    uint ThreadGroupCountX;
    uint ThreadGroupCountY;
    uint ThreadGroupCountZ;
};

RWStructuredBuffer<EmitterData> Emitter : register(u1);
RWStructuredBuffer<DispatchArgs> Args : register(u4);
RWStructuredBuffer<GpuMouseBuffer> Counters : register(u3);

[numthreads(1,1,1)]
void main(uint3 tid : SV_DispatchThreadID)
{
    EmitterData data = Emitter[0];
    float dist = length(mousePos - mousePosPrev);

    uint increment = (uint)( (SpawnRate * DeltaTime + SpawnRatePerUnit * dist ) * 65536.0) ;

    uint acc = data.SpawnAccumulator + increment;
    uint spawnCount = acc >> 16;
    acc &= 0xFFFF;

    data.SpawnAccumulator = acc;
    data.SpawnCountThisFrame = spawnCount;
    data.ConsumedSpawns = 0;
    uint totalCount = data.AliveCount + data.SpawnCountThisFrame;
    Args[0].ThreadGroupCountX = (totalCount + 255) / 256;
    Args[0].ThreadGroupCountY = 1;
    Args[0].ThreadGroupCountZ = 1;

    Emitter[0] = data;

    Counters[0].VelocityBlend = exp(-10 * DeltaTime);
}
