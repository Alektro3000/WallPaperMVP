#include "common.hlsli"

Texture2D DebugTexture : register(t0);
SamplerState DebugSampler : register(s0);

struct VsOut
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD0;
};

VsOut MAIN_VS(uint vertexID : SV_VertexID)
{
    VsOut o;
    uint indeces[6] = {
        0,1,2,1,2,3
    };

    float2 positions[4] =
    {
        float2(-1.0, -1.0),
        float2(-1.0,  1.0),
        float2( 1.0, -1.0),
        float2( 1.0,  1.0)
    };

    float2 uvs[4] =
    {
        float2(0.0, 0.0),
        float2(0.0, 1.0),
        float2(1.0, 0.0),
        float2(1.0, 1.0)
    };

    o.Position = float4(positions[indeces[vertexID]] * DebugSettings.Size + DebugSettings.Center, 0.0, 1.0);
    o.UV = uvs[indeces[vertexID]];
    return o;
}

float4 MAIN_PS(float4 position : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float4 field = DebugTexture.SampleLevel(DebugSampler, uv, 0);
    if(DebugSettings.ShowVelocity < 0.5f)
    {
        float sdf = field[(uint)DebugSettings.MaskId];
        float output = saturate((sdf - DebugSettings.minPos) / (DebugSettings.maxPos - DebugSettings.minPos));
        return float4(lerp(DebugSettings.minColor, DebugSettings.maxColor, output), 1.0f);
    }

    return float4((field.xy - DebugSettings.minPos) / (DebugSettings.maxPos - DebugSettings.minPos), 1, 1.0f);
}
