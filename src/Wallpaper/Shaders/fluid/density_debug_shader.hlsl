#include "common.hlsli"

StructuredBuffer<Particle> Particles : register(t0);
StructuredBuffer<HashEntry> HashEntriesSrv : register(t4);
StructuredBuffer<CellRange> CellRangesSrv : register(t5);

struct VsOut
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
};

struct PsIn
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
};

VsOut MAIN_VS(uint vertexId : SV_VertexID)
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

float4 MAIN_PS(PsIn input) : SV_TARGET
{
    float2 world = float2((input.Uv.x * 2.0f - 1.0f) * ScreenRatio, input.Uv.y * 2.0f - 1.0f);
    int2 cell = CellCoord(world);
    float h = InfluenceRadius;
    float density = 0.0f;

    [unroll]
    for (int oy = -1; oy <= 1; oy++)
    {
        [unroll]
        for (int ox = -1; ox <= 1; ox++)
        {
            int2 ncell = cell + int2(ox, oy);

            CellRange range = CellRangesSrv[CellHash(ncell)];
            for (uint n = range.Start; n < range.End; n++)
            {
                Particle p = Particles[HashEntriesSrv[n].ParticleIndex];
                float r = length(world - p.Position);
                if(r < h)
                    density += SphKernel(r, h);
            }
        }
    }

    float v = saturate((density - DensityDebugMin) / max(DensityDebugMax - DensityDebugMin, 0.01f));
    return float4(v.xxx, 1.0f);
}
