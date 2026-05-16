
cbuffer buffer : register(b0)
{
    row_major float4x4 ModelViewProjection;
}

cbuffer buffer : register(b2)
{
    float4x4 Joints[640];
}

struct VSInput
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
    float4 tangent  : TANGENT;
    float2 UV       : TEXCOORD;

    float4 Weights  : WEIGHTS;
    uint4  Joints   : JOINTS; 
};

struct VSOut
{
    float4 position : SV_Position;
    float3 normal   : NORMAL;
    float2 UV    : TEXCOORD;
};

// Simple working vertex shader
VSOut main(VSInput input)
{
    VSOut o;

    o.position = mul(float4(input.position, 1.0), ModelViewProjection);
    o.normal = input.normal;

    o.UV = input.UV;
    return o;
}