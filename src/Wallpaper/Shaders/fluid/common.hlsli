#include "../common/commonCompute.hlsli"

cbuffer Constants : register(b0)
{
    uint ParticleCount;
    uint RangeCount;
    float2 MousePos;

    float3 Color;
    float Size;

    float GridSize;
    float InfluenceRadius;
    float RestDensity;
    float Pressure;

    float Viscosity;
    float Gravity;
    float WindowsForce;
    float WindowsOffset;
    float SoftBoundaryScale;

    float BoundaryHardness;
    float BoundaryForce;
    float SeparationRadius;
    float SeparationStrength;
    
    float DensityDebug;
    float DensityDebugMin;
    float DensityDebugMax;
};

struct HashEntry
{
    uint Hash;
    uint ParticleIndex;
};

struct CellRange
{
    uint Start;
    uint End;
};

uint2 CellCoord(float2 p)
{
    float2 snapped = SnapToGrid(p, GridSize);
    float2 normalized = (snapped + float2(ScreenRatio, 1.0f)) / GridSize;
    int2 cell = int2(floor(normalized + 0.5f));
    return cell;
}

uint2 Mod4(int2 v)
{
    return uint2(v) & 3u;
}

uint CellHash(int2 cell)
{
    uint2 local = Mod4(cell);
    uint localId = (local.y << 2) | local.x; // 0..15

    uint h = WangHash(cell.y) ^ WangHash(cell.x * 3 + 1);
    uint hash = (h << 4) | localId;
    
    return hash & (RangeCount-1);
}

uint CellHashFromPosition(float2 p)
{
    return CellHash(CellCoord(p));
}

float SphKernel(float r, float h)
{
    float q = saturate(1.0f - r / h);
    return q * q * q;
}

float SphKernel2(float r, float h)
{
    float q = saturate(1.0f - r / h);
    return q * q;
}
