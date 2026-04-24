#include "../common/commonCompute.hlsli"


cbuffer Constants : register(b0)
{
    float2 CatmulA;
    float2 CatmulB;
    float2 CatmulC;
    float2 CatmulD;

    uint ParticleCount;
    float VelocityBlend;
    float2 MousePos;

    float MouseFrameDistance;
    float PhaseShift;
    float WaveCyclesOnSegment;
    float MouseSpeed;
    
    float3 Color;
    float Size;
    
    float2 GridSize;
    float Radial;
    float Tangent;

    float LifeTime;
    float SpawnRate;
    float SpawnRatePerUnit;
    float InitVelocity;

    float StationaryLerpStart;
    float StationaryLerpEnd;
    float OffsetLerpStart;
    float OffsetLerpEnd;

    float StripWidth;
    float SparkPercent;
    float StationaryVelocity;
};