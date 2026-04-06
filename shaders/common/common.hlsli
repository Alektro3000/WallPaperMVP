#ifndef COMMON_INCLUDED
#define COMMON_INCLUDED

static const float PI = 3.14159265359f;
static const float PI2 = 3.14159265359f*2;


struct Particle
{
    float4 Color;
    float2 Position;
    float2 Velocity;
    float Size;
    float Age;
    float2 CustomData;
    float4 CustomData1;
};

struct EmitterData{
     uint SpawnCountThisFrame;
     uint ConsumedSpawns;
     uint SpawnAccumulator;
};

uint WangHash(uint s)
{
    s = (s ^ 61u) ^ (s >> 16);
    s *= 9u;
    s = s ^ (s >> 4);
    s *= 0x27d4eb2du;
    s = s ^ (s >> 15);
    return s;
}

float Random(uint seed)
{
    return WangHash(seed) / 4294967296.0;
}
float2 Random2(uint seed)
{
    return float2(WangHash(seed) / 4294967296.0, WangHash(seed + 120) / 4294967296.0);
}

float2 Rotate(float2 v, float angle)
{
    float c = cos(angle);
    float s = sin(angle);

    return float2(
        v.x * c - v.y * s,
        v.x * s + v.y * c
    );
}
float2 Rotate(float v, float angle)
{
    float c = cos(angle);
    float s = sin(angle);

    return float2(
        v.x * c,
        v.x * s
    );
}
#endif