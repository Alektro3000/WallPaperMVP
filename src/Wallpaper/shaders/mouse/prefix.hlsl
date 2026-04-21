#include "common.hlsli"

RWStructuredBuffer<uint> ActiveList : register(u2);

[numthreads(1, 1, 1)]
void main(uint3 tid : SV_DispatchThreadID)
{
    if (tid.x != 0)
        return;

    uint sum = 0;

    for (uint i = 0; i < ParticleCount; ++i)
    {
        uint flag = ActiveList[i];   // expected 0 or 1 from alive.hlsl
        ActiveList[i] = sum;         // exclusive prefix
        sum += flag;
    }
}