
cbuffer Constants : register(b0)
{
    float4 TintColor;
};

struct VSInput
{
    float2 localOffset : POSITION;
    float2 uv          : TEXCOORD;

    float3 position   : INSTANCE_POSITION;
    float3 velocity   : INSTANCE_VELOCITY;
    float3 color    : INSTANCE_COLOR;
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

    float3 worldPos = input.position + float3(input.localOffset * 0.1, 0.0);
    o.position = float4(worldPos, 1.0);

    o.color = float4(input.color, 1.0) * TintColor;
    return o;
}