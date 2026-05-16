#include "common.hlsli"

struct WindowFieldDescription
{
    float2 PrevMin;
    float2 PrevMax;
    float2 CurrMin;
    float2 CurrMax;
    float2 ExtendedMin;
    float2 ExtendedMax;
};
cbuffer FieldWindowDescriptors : register(b2)
{
    WindowFieldDescription Descriptors[32];
}

RWTexture2D<float4> FieldUav : register(u0);

float InsideBox(float2 p, float2 bmin, float2 bmax)
{
    return all(p >= bmin && p <= bmax) ? 1.0 : 0.0;
}

float WindowSdf(float2 p, float2 bmin, float2 bmax)
{
    float2 center = (bmin + bmax) * 0.5f;
    float2 halfSize = (bmax - bmin) * 0.5f;

    float2 q = abs(p - center) - halfSize;

    float outsideDist = length(max(q, 0.0f));
    float insideDist = min(max(q.x, q.y), 0.0f);

    return outsideDist + insideDist;
}

[numthreads(8, 8, 1)]
void MAIN_CS(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 xy = dispatchThreadId.xy;
    if(any(xy >= fieldSize))
        return;

    float2 pos = xy;
    float2 velocity = 0;
    float1 inside = 0;
    float sdf = 1e20;
    float2 velocitySDF = 0.0f;

    for(uint i = 0; i < WindowCount; i++)
    {
        WindowFieldDescription desc = Descriptors[i];


        float2 prevCenter = 0.5 * (desc.PrevMin + desc.PrevMax);
        float2 currCenter = 0.5 * (desc.CurrMin + desc.CurrMax);
        float2 motion = currCenter - prevCenter;
        
        float2 closest = clamp(pos, desc.CurrMin, desc.CurrMax);
        float2 closestPrev = clamp(pos, desc.PrevMin, desc.PrevMax);

        float2 edgeSpeed = closest - closestPrev;

        float2 delta = pos - closest;

        float dist = length(delta);

        float t = saturate(1.0 - dist / InfluenceRadius);
        float falloff = t * t;
        velocity += (edgeSpeed * EdgeSpeed + motion * WindowSpeed) * falloff;
        
        float boxSdf = WindowSdf(pos, 
                desc.ExtendedMin,
                desc.ExtendedMax);

        sdf = min(sdf, boxSdf);
    }
    FieldUav[xy] = float4(velocity, sdf, sdf );
}