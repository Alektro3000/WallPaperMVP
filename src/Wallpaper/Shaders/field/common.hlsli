#include "../common/common.hlsli"


struct DebugSettingsStruct
{
    float2 Center;
    float2 Size;

    float3 minColor;
    float  minPos;

    float3 maxColor;
    float  maxPos;

    float MaskId;
    float ShowVelocity;
    float2 _padding1;
};

cbuffer FieldConstantBuffer : register(b0)
{
    DebugSettingsStruct DebugSettings;
    uint WindowCount;
    int3 _padding;
};