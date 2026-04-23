#include "common.hlsli"

Texture2D<float4> FieldSrv : register(t1);
SamplerState LinearSampler : register(s0);


Particle updateParticleField(Particle p)
{
    
    float2 texel = float2( 
        (p.Position.x * ScreenRatioInv),
        (p.Position.y)
        ) * 0.5 + 0.5;
    p.Velocity += FieldSrv.SampleLevel(LinearSampler,texel,0).xy;
    
    return p;
};

struct DispatchArgs
{
    uint ThreadGroupCountX;
    uint ThreadGroupCountY;
    uint ThreadGroupCountZ;
};

struct DrawIndexedArgs
{
    uint IndexCountPerInstance;
    uint InstanceCount;
    uint StartIndexLocation;
    int  BaseVertexLocation;
    uint StartInstanceLocation;
};
