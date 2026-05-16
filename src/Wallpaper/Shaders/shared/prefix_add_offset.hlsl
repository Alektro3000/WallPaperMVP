#include "../common/commonCompute.hlsli"

RWStructuredBuffer<uint> ActiveList : register(u2);
StructuredBuffer<uint> BlockSums    : register(t3);

RWStructuredBuffer<EmitterData> Emitter : register(u1);

[numthreads(256, 1, 1)]
void MAIN_CS(
    uint3 dtid : SV_DispatchThreadID,
    uint3 gid  : SV_GroupID)
{

    uint globalId = dtid.x;
    if(globalId >= Emitter[0].TotalCount)
        return;

    uint groupOffset = BlockSums[gid.x];
    ActiveList[globalId] += groupOffset;
}