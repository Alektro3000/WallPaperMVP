#include "common.hlsli"

RWStructuredBuffer<EmitterData> Emitter : register(u1);
RWStructuredBuffer<DispatchArgs> DispatchArgsBuffer : register(u4);

[numthreads(1, 1, 1)]
void main(uint3 dtid : SV_DispatchThreadID)
{
    EmitterData emitter = Emitter[0];
    uint alive = min(emitter.AliveCountCheck, ParticleCount);
    uint spawn = min((uint)(SpawnRate * DeltaTime), ParticleCount - alive);

    emitter.AliveCount = alive;
    emitter.SpawnCountThisFrame = spawn;
    emitter.TotalCount = ParticleCount;
    emitter.ConsumedSpawns = 0;
    emitter.AliveCountCheck = 0;
    Emitter[0] = emitter;

    DispatchArgs args;
    args.ThreadGroupCountX = (emitter.TotalCount + 255) / 256;
    args.ThreadGroupCountY = 1;
    args.ThreadGroupCountZ = 1;
    DispatchArgsBuffer[0] = args;
}
