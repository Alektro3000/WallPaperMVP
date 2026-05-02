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
    uint2 ScreenSize;
    uint WindowCount;
};

float InsideBox(float2 p, float2 bmin, float2 bmax)
{
    return all(p >= bmin && p <= bmax) ? 1.0 : 0.0;
}

float WindowScreenAwareSdf(float2 p, float2 bmin, float2 bmax)
{
    float sdf = 1e20f;

    bool useLeft   = bmin.x > 0.0f;
    bool useRight  = bmax.x < fieldSize.x;
    bool useBottom = bmin.y > 0.0f;
    bool useTop    = bmax.y < fieldSize.y;


    // inside: negative distance to nearest active face
    if (p.x >= bmin.x && p.x <= bmax.x &&
        p.y >= bmin.y && p.y <= bmax.y)
    {
        float insideDist = 1e20f;

        if (useLeft)   insideDist = min(insideDist, p.x - bmin.x);
        if (useRight)  insideDist = min(insideDist, bmax.x - p.x);
        if (useBottom) insideDist = min(insideDist, p.y - bmin.y);
        if (useTop)    insideDist = min(insideDist, bmax.y - p.y);

        if (insideDist < 1e19f)
            sdf = min(sdf, -insideDist);
    }

    return sdf;
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

        float2 edgeSpeed = closest - closestPrev ;

        float2 delta = pos - closest;

        float dist = length(delta);
        float influenceRadius = 5.0;

        float t = saturate(1.0 - dist / influenceRadius);
        float falloff = t * t;
        velocity += (edgeSpeed * 0.02f + motion * 0.005f) * falloff;
        
        float boxSdf = WindowScreenAwareSdf(pos, 
                desc.CurrMin,
                desc.CurrMax);

        sdf = min(sdf, boxSdf);
    }
    FieldUav[xy] = float4(velocity, sdf, sdf );
}