#pragma pack_matrix(row_major)

cbuffer ModelConstants : register(b0)
{
    float4x4 ViewProjection;
}

cbuffer MaterialBuffer : register(b1)
{
    float3 albedoColor;
    int albedoTextureIndex;
    int normalTextureIndex;
    uint flags;
};

Texture2D TextureHeap[] : register(t0);
SamplerState LinearSampler : register(s0);

#ifdef SKELETAL
cbuffer JointsConstants : register(b2)
{
    float4x4 Joints[1024];
}
#endif

struct VSInput
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
    float4 tangent  : TANGENT;
    float2 UV       : TEXCOORD;

#ifdef SKELETAL
    float4 Weights  : WEIGHTS;
    uint4  Joints   : JOINTS;
#endif
};

struct PSInput
{
    float4 Position : SV_Position;
    float3 normal   : NORMAL;
    float2 UV       : TEXCOORD;
};

PSInput MAIN_VS(VSInput input)
{
    PSInput o;

    float4 localPosition = float4(input.position, 1.0);
    float4 localNormal = float4(input.normal, 0.0);

#ifdef SKELETAL
    float4 skinnedPosition =
        mul(localPosition, Joints[input.Joints.x]) * input.Weights.x +
        mul(localPosition, Joints[input.Joints.y]) * input.Weights.y +
        mul(localPosition, Joints[input.Joints.z]) * input.Weights.z +
        mul(localPosition, Joints[input.Joints.w]) * input.Weights.w;
    float4 skinnedNormal = normalize(
        mul(localNormal, Joints[input.Joints.x]) * input.Weights.x +
        mul(localNormal, Joints[input.Joints.y]) * input.Weights.y +
        mul(localNormal, Joints[input.Joints.z]) * input.Weights.z +
        mul(localNormal, Joints[input.Joints.w]) * input.Weights.w);
#else
    float4 skinnedPosition = localPosition;
    float4 skinnedNormal = localNormal;
#endif

    o.Position = mul(skinnedPosition, ViewProjection);
    o.normal = skinnedNormal.xyz;
    o.UV = input.UV;
    return o;
}

float4 MAIN_PS(PSInput input) : SV_Target
{
    if ((flags & 8) != 0)
    {
        return float4(input.normal / 2 + 0.5, 1);
    }
    if ((flags & 4) != 0)
    {
        return float4(input.UV, 0, 1);
    }
    if ((flags & 1) != 0)
    {
        return TextureHeap[albedoTextureIndex].Sample(LinearSampler, input.UV);
    }
    if ((flags & 2) != 0)
    {
        return float4(albedoColor, 1);
    }
    return float4(input.UV, 0, 1);
}
