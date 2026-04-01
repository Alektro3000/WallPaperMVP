struct Particle
{
    float3 Position;
    float3 Velocity;
    float3 color;
};

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<Particle> NextParticles : register(u0);

cbuffer Constants : register(b0)
{
    float4 TintColor;
    uint ParticleCount;
    float DeltaTime;
    float2 Mouse;
};

[numthreads(256, 1, 1)] void main(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;
    if (i >= ParticleCount)
        return;

    Particle p = PrevParticles[i];

    {
        float3 diff = float3(Mouse, 0) - p.Position;
        float3 norm = normalize(diff);
        float vecLength = length(diff);
        if(vecLength > 0.1f)
            p.Velocity += norm * DeltaTime ;
    }

    for (int j = 0; j < ParticleCount; j++)
        if (i != j)
        {
            float3 diff = PrevParticles[j].Position - p.Position;
            float3 norm = normalize(diff);
            float vecLength = length(diff);
            if(vecLength > 0.1f)
                p.Velocity +=  norm * DeltaTime ;
            // if(vecLength < 0.1)
            //     p.Velocity -= 100 * (0.1-vecLength) *norm * DeltaTime;
        }

    p.Velocity *= 0.999;
    p.Position = p.Position + p.Velocity * DeltaTime * 0.01;

    NextParticles[i] = p;
}