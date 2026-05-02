Texture2D DebugTexture : register(t0);
SamplerState DebugSampler : register(s0);

float4 main(float4 position : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float4 field = DebugTexture.SampleLevel(DebugSampler, uv, 0);
    float sdf = field.z;

    if (sdf > 1e10f)
        return float4(abs(field.xy) * 10.0f, 0.0f, 1.0f);

    float edge = saturate(0.5f - sdf * 0.05f);
    return float4(edge, edge, edge, 1.0f);
}
