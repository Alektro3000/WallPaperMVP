#include "common.hlsli"

Texture2D DebugTexture : register(t0);
SamplerState DebugSampler : register(s0);

float4 main(float4 position : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float4 field = DebugTexture.SampleLevel(DebugSampler, uv, 0);
    if(DebugSettings.ShowVelocity < 0.5f)
    {
        float sdf = field[(uint)DebugSettings.MaskId];
        float edge = saturate(0.5f - sdf * 0.05f);
        float output = saturate( (sdf-DebugSettings.minPos) / (DebugSettings.maxPos - DebugSettings.minPos));
        return float4(lerp(DebugSettings.minColor,DebugSettings.maxColor, output), 1.0f);
    }
        return float4((field.xy-DebugSettings.minPos) / (DebugSettings.maxPos - DebugSettings.minPos), 1, 1.0f);

}
