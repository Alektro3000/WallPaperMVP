#include "../common/commonCompute.hlsli"

RWStructuredBuffer<EmitterData> Emitter : register(u1);

StructuredBuffer<Particle> SourceParticles : register(t0);
RWStructuredBuffer<Particle> DestParticles : register(u0);
RWStructuredBuffer<uint> AliveList: register(u2);


[numthreads(1,1,1)]
void main(uint3 tid : SV_DispatchThreadID)
{
    if (tid.x != 0)
        return;

    int id = 0;
    for(int i = 0; i < Emitter[0].TotalCount; i++)
    {
        Particle p = SourceParticles[i];
        
        AliveList[i] = id;
        if(p.Age >= 0)
        {
            DestParticles[id++] = p;
        }
    }
}
