#include "common.hlsli"

RWStructuredBuffer<CellRange> CellRanges : register(u7);

[numthreads(256, 1, 1)]
void MAIN_CS(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;
    if (i >= RangeCount)
        return;

    CellRange emptyRange;
    emptyRange.Start = 0;
    emptyRange.End = 0;
    CellRanges[i] = emptyRange;
}
