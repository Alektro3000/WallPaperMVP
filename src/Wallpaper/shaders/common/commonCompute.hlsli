#include "common.hlsli"

Texture2D<float4> FieldSrv : register(t1);
SamplerState LinearSampler : register(s0);

Particle updateParticleField(Particle p)
{

    float2 texel = float2(
                       (p.Position.x * ScreenRatioInv),
                       (p.Position.y)) *
                       0.5 +
                   0.5;
    p.Velocity += FieldSrv.SampleLevel(LinearSampler, texel, 0).xy;

    return p;
};

float2 getMoveOutWindowForce(float2 Pos, float windowOffset)
{
    float2 uv = float2(
        Pos.x * ScreenRatioInv,
        Pos.y
    ) * 0.5f + 0.5f;

    float sdf = FieldSrv.SampleLevel(LinearSampler, uv, 0).w;

    if(sdf > windowOffset)
        return 0;

    float2 texel = float2(ScreenRatioInv, 1.0f) * 0.01f;

    float sdfL = FieldSrv.SampleLevel(LinearSampler, uv - float2(texel.x, 0), 0).z;
    float sdfR = FieldSrv.SampleLevel(LinearSampler, uv + float2(texel.x, 0), 0).z;
    float sdfD = FieldSrv.SampleLevel(LinearSampler, uv - float2(0, texel.y), 0).z;
    float sdfU = FieldSrv.SampleLevel(LinearSampler, uv + float2(0, texel.y), 0).z;

    float2 grad = normalize(float2(sdfR - sdfL, sdfU - sdfD) + 1e-6f);

    return grad;
}

struct DispatchArgs
{
    uint ThreadGroupCountX;
    uint ThreadGroupCountY;
    uint ThreadGroupCountZ;
};

struct DrawIndexedArgs
{
    uint IndexCountPerInstance;
    uint InstanceCount;
    uint StartIndexLocation;
    int BaseVertexLocation;
    uint StartInstanceLocation;
};
