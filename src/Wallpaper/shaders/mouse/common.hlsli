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
    
    float Size;
    float2 GridSize;
    uint _padding;

    float3 BeginColor;
    uint _padding1;

    float3 EndColor;
    float InitVelocity;

    float LifeTime;
    float SpawnRate;
    float SpawnRatePerUnit;
    float SparkPercent;

    float OffsetLerpStart;
    float OffsetLerpEnd;
    float StripWidth;
    uint _padding2;

    float StationaryLerpStart;
    float StationaryLerpEnd;
    float StationaryVelocity;
    float StationaryOffset;
};
