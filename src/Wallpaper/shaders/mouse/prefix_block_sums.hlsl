#include "common.hlsli"

RWStructuredBuffer<uint> BlockSums : register(u3);


RWStructuredBuffer<EmitterData> Emitter : register(u1);
groupshared uint WaveTotals[8];
groupshared uint WaveOffsets[8];

[numthreads(256, 1, 1)]
void main(
    uint3 dtid : SV_DispatchThreadID,
    uint3 gtid : SV_GroupThreadID)
{
    uint totalCount = Emitter[0].TotalCount;

    uint localId = gtid.x;
    uint groupCount = (totalCount + 255) / 256;

    uint value = (localId < groupCount) ? BlockSums[localId] : 0;

    uint waveSize  = WaveGetLaneCount();
    uint laneIndex = WaveGetLaneIndex();
    uint waveIndex = localId / waveSize;
    uint waveCount = (256 + waveSize - 1) / waveSize;

    uint exclusive = WavePrefixSum(value);

    uint waveTotal = WaveActiveSum(value);

    if (WaveIsFirstLane())
        WaveTotals[waveIndex] = waveTotal;

    GroupMemoryBarrierWithGroupSync();

    if (waveIndex == 0)
    {
        uint v = (laneIndex < waveCount) ? WaveTotals[laneIndex] : 0;

        uint exc = WavePrefixSum(v);

        if (laneIndex < waveCount)
            WaveOffsets[laneIndex] = exc;
    }

    GroupMemoryBarrierWithGroupSync();

    if (localId < groupCount)
    {
        BlockSums[localId] = WaveOffsets[waveIndex] + exclusive;
    }
}