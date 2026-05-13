cbuffer MaterialBuffer : register(b1)
{
    uint flags;
};

Texture2D AlbedoTexture : register(t0);
SamplerState LinearSampler : register(s0);


struct PSInput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD;
};


float4 main(PSInput input) : SV_Target
{
    if ((flags & 1) == 0)
    {
        return float4(input.UV, 0, 1);
    }
    return AlbedoTexture.SampleLevel(LinearSampler, input.UV, 0);
}