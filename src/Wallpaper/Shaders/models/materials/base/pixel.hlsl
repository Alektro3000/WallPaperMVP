cbuffer MaterialBuffer : register(b1)
{
    uint flags;
};

Texture2D AlbedoTexture : register(t0);
Texture2D NormalTexture : register(t1);
SamplerState LinearSampler : register(s0);


struct PSInput
{
    float4 Position : SV_Position;
    float3 normal   : NORMAL;
    float2 UV       : TEXCOORD;
};


float4 main(PSInput input) : SV_Target
{
    if ((flags & 8) != 0)
    {
        return float4(input.normal/2 + 0.5, 1);
    }
    if (((flags & 1) == 0) || ((flags & 4) != 0))
    {
        return float4(input.UV, 0, 1);
    }
    return AlbedoTexture.Sample(LinearSampler, input.UV);
}