#pragma pack_matrix(row_major)

cbuffer ModelConstants : register(b0)
{
    float4x4 ViewProjection;
}

cbuffer JointsConstants : register(b2)
{
    float4x4 Joints[1024];
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


    float4 localPosition = float4(input.position, 1.0);
    float4 localNormal = float4(input.normal, 0.0);

    float4 skinnedPosition =
        mul(localPosition, Joints[input.Joints.x]) * input.Weights.x +
        mul(localPosition, Joints[input.Joints.y]) * input.Weights.y +
        mul(localPosition, Joints[input.Joints.z]) * input.Weights.z +
        mul(localPosition, Joints[input.Joints.w]) * input.Weights.w;

    float4 skinnedNormal =
        mul(localNormal, Joints[input.Joints.x]) * input.Weights.x +
        mul(localNormal, Joints[input.Joints.y]) * input.Weights.y +
        mul(localNormal, Joints[input.Joints.z]) * input.Weights.z +
        mul(localNormal, Joints[input.Joints.w]) * input.Weights.w;

    o.Position = mul(skinnedPosition, ViewProjection);
    o.normal = skinnedNormal.xyz;
    o.UV = input.UV;
    return o;
}

float4 MAIN_PS(PSInput input) : SV_Target
{
    if ((flags & 8) != 0)
    {
        return float4(input.normal/2+0.5, 1);
    }
    if (((flags & 4) != 0))
    {
        return float4(input.UV, 0, 1);
    }
    if ((flags & 1) == 0) 
    {
        clip(-1);
    }
    return AlbedoTexture.Sample(LinearSampler, input.UV);
}
