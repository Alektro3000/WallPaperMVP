#include "common.hlsli"

RWStructuredBuffer<EmitterData> Emitter : register(u1);
RWStructuredBuffer<uint> ActiveList : register(u2);
RWStructuredBuffer<uint> BlockSums  : register(u3);

static const uint THREADS_PER_GROUP = 256;
static const uint MAX_WAVES_PER_GROUP = THREADS_PER_GROUP / 32;

groupshared uint WaveTotals[MAX_WAVES_PER_GROUP]; 
groupshared uint WaveOffsets[MAX_WAVES_PER_GROUP];

[numthreads(THREADS_PER_GROUP, 1, 1)]
void main(uint3 dtid  : SV_DispatchThreadID,
          uint3 gtid  : SV_GroupThreadID,
          uint3 gid   : SV_GroupID)
{
    uint totalCount = Emitter[0].TotalCount;

    uint globalId = dtid.x;
    uint localId  = gtid.x;

    uint flag = 0;
    if (globalId < totalCount)
        flag = ActiveList[globalId];

    uint laneIndex = WaveGetLaneIndex();
    uint waveSize  = WaveGetLaneCount();
    uint waveIndex = localId / waveSize;

    // inclusive prefix inside wave
    uint exclusive = WavePrefixSum(flag);

    // total sum of this wave
    uint waveTotal = WaveActiveSum(flag);

    // one lane per wave writes wave total
    if (WaveIsFirstLane())
    {
        WaveTotals[waveIndex] = waveTotal;
    }

    GroupMemoryBarrierWithGroupSync();

    // first wave scans wave totals
    uint waveCount = (256 + waveSize - 1) / waveSize;

    if (waveIndex == 0)
    {
        uint v = (laneIndex < waveCount) ? WaveTotals[laneIndex] : 0;

        uint exc = WavePrefixSum(v);

        if (laneIndex < waveCount)
            WaveOffsets[laneIndex] = exc;
    }

    GroupMemoryBarrierWithGroupSync();

    uint blockOffset = WaveOffsets[waveIndex];
    uint result = blockOffset + exclusive;

    if (globalId < totalCount)
        ActiveList[globalId] = result;

    // last valid thread in block writes block sum
    uint groupStart = gid.x * 256;
    uint validCount = (groupStart < totalCount) ? min(256, totalCount - groupStart) : 0;

    if (validCount > 0 && localId == validCount - 1)
    {
        BlockSums[gid.x] = result + flag;
    }
}