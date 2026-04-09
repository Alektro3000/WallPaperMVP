#include "common/common.hlsli"

RWTexture2D<float4> FieldUav : register(u0);

struct WindowFieldDescription
{
    float2 PrevMin;
    float2 PrevMax;
    float2 CurrMin;
    float2 CurrMax;
};

cbuffer FieldConstantBuffer : register(b0)
{
    WindowFieldDescription Descriptors[32];
    uint WindowCount;
    uint2 WindowSize;
};

float InsideBox(float2 p, float2 bmin, float2 bmax)
{
    return all(p >= bmin && p <= bmax) ? 1.0 : 0.0;
}
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 xy = dispatchThreadId.xy;
    if(any(xy >= fieldSize))
        return;

    float2 pos = xy;
    float2 velocity = 0;
    float1 inside = 0;
    for(uint i = 0; i < WindowCount; i++)
    {
        WindowFieldDescription desc = Descriptors[i];

        float2 prevCenter = 0.5 * (desc.PrevMin + desc.PrevMax);
        float2 currCenter = 0.5 * (desc.CurrMin + desc.CurrMax);
        float2 motion = currCenter - prevCenter;
        
        float2 closest = clamp(pos, desc.CurrMin, desc.CurrMax);
        float2 closestPrev = clamp(pos, desc.PrevMin, desc.PrevMax);

        float2 edgeSpeed = closest - closestPrev ;

        float2 delta = pos - closest;

        float dist = length(delta);
        float influenceRadius = 5.0;

        float t = saturate(1.0 - dist / influenceRadius);
        float falloff = t * t;
        velocity += (edgeSpeed * 0.02f + motion * 0.005f) * falloff;
        inside += InsideBox(pos, desc.CurrMin, desc.CurrMax);
    }
    FieldUav[xy] = float4(velocity, saturate(inside),1);
}