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

    float2 worldPos = part.Position + input.localOffset * (part.Size);
    o.position = mul(float4(worldPos, 0, 1.0), ViewMatrix);

    o.color = part.Color;
    return o;
}