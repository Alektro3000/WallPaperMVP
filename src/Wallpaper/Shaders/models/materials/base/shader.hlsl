cbuffer ModelConstants : register(b0)
{
    row_major float4x4 ModelViewProjection;
}

cbuffer JointsConstants : register(b2)
{
    float4x4 Joints[640];
}

cbuffer MaterialBuffer : register(b1)
{
    uint flags;
};

Texture2D AlbedoTexture : register(t0);
Texture2D NormalTexture : register(t1);
SamplerState LinearSampler : register(s0);

struct VSInput
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
    float4 tangent  : TANGENT;
    float2 UV       : TEXCOORD;

    float4 Weights  : WEIGHTS;
    uint4  Joints   : JOINTS;
};

struct PSInput
{
    float4 Position : SV_Position;
    float3 normal   : NORMAL;
    float4 tangent  : TANGENT;
    float2 UV       : TEXCOORD;
};

PSInput MAIN_VS(VSInput input)
{
    PSInput o;
    o.Position = mul(float4(input.position, 1.0), ModelViewProjection);
    o.normal = input.normal;
    o.tangent = input.tangent;
    o.UV = input.UV;
    return o;
}

float4 MAIN_PS(PSInput input) : SV_Target
{
    if ((flags & 8) != 0)
    {
        return float4(input.normal/2+0.5, 1);
    }
    if (((flags & 1) == 0) || ((flags & 4) != 0))
    {
        return float4(input.UV, 0, 1);
    }
    return AlbedoTexture.Sample(LinearSampler, input.UV);
}
