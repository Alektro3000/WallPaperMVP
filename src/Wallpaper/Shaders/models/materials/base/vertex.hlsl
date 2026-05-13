
cbuffer buffer : register(b0)
{
    row_major float4x4 ModelViewProjection;
}

struct VSInput
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
    float4 tangent  : TANGENT;
    float2 UV       : TEXCOORD;
};

struct VSOut
{
    float4 position : SV_Position;
    float2 UV    : TEXCOORD;
};

// Simple working vertex shader
VSOut main(VSInput input)
{
    VSOut o;

    o.position = mul(float4(input.position, 1.0), ModelViewProjection);

    o.UV = input.UV;
    return o;
}