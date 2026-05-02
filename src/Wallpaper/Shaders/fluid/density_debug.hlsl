struct VsOut
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
};

VsOut main(uint vertexId : SV_VertexID)
{
    float2 vertices[6] =
    {
        float2(-1.0f, -1.0f),
        float2(-1.0f,  1.0f),
        float2( 1.0f,  1.0f),
        float2(-1.0f, -1.0f),
        float2( 1.0f,  1.0f),
        float2( 1.0f, -1.0f)
    };

    VsOut output;
    output.Position = float4(vertices[vertexId], 0.0f, 1.0f);
    output.Uv = vertices[vertexId] * 0.5f + 0.5f;
    return output;
}
