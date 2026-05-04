#include "common.hlsli"

struct VSOut
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD0;
};

VSOut main(uint vertexID : SV_VertexID)
{
    VSOut o;
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