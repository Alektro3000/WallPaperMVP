#include "common/common.hlsli"

StructuredBuffer<Particle> Particles : register(t0);

struct VSInput
{
    float2 localOffset : POSITION;
    float2 uv          : TEXCOORD;

    uint index   : SV_InstanceID;
};

struct VSOut
{
    float4 position : SV_Position;
    float4 color    : COLOR;
};

// Simple working vertex shader
VSOut main(VSInput input)
{
    VSOut o;

    Particle part = Particles[input.index];

    float3 worldPos = part.Position + float3(input.localOffset * (part.Size), 0.0);
    o.position = mul(float4(worldPos, 1.0), viewMatrix);

    o.color = float4(part.Color);
    return o;
}