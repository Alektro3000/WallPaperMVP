Texture2D DebugTexture : register(t0);
SamplerState DebugSampler : register(s0);

float4 main(float4 position : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    return float4(DebugTexture.SampleLevel(DebugSampler, uv, 0).xy * 10,0,1);
}