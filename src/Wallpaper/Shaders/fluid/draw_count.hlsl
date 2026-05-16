#include "common.hlsli"

RWStructuredBuffer<EmitterData> Emitter : register(u1);
RWStructuredBuffer<DrawIndexedArgs> DrawArgs : register(u5);

[numthreads(1, 1, 1)]
void MAIN_CS(uint3 dtid : SV_DispatchThreadID)
{
    EmitterData emitter = Emitter[0];
    DrawIndexedArgs args;
    args.IndexCountPerInstance = 6;
    args.InstanceCount = ParticleCount;
    args.StartIndexLocation = 0;
    args.BaseVertexLocation = 0;
    args.StartInstanceLocation = 0;
    DrawArgs[0] = args;
}
