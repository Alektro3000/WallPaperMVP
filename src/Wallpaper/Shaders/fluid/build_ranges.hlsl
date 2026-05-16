#include "common.hlsli"

StructuredBuffer<HashEntry> HashEntriesSrv : register(t4);
RWStructuredBuffer<CellRange> CellRanges : register(u7);

[numthreads(256, 1, 1)]
void MAIN_CS(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;
    if (i >= ParticleCount)
        return;

    HashEntry entry = HashEntriesSrv[i];
    if (entry.Hash >= RangeCount)
        return;

    if (i == 0 || HashEntriesSrv[i - 1].Hash != entry.Hash)
        CellRanges[entry.Hash].Start = i;

    if (i == ParticleCount - 1 || HashEntriesSrv[i + 1].Hash != entry.Hash)
        CellRanges[entry.Hash].End = i + 1;
}
