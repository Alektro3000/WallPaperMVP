#include "common.hlsli"

RWStructuredBuffer<CellRange> CellRanges : register(u7);

[numthreads(256, 1, 1)]
void main(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;
    if (i >= RangeCount)
        return;

    CellRange emptyRange;
    emptyRange.Start = 0;
    emptyRange.Count = 0;
    CellRanges[i] = emptyRange;
}
