#include "common.hlsli"

StructuredBuffer<Particle> PrevParticles : register(t0);
RWStructuredBuffer<HashEntry> HashEntries : register(u6);

cbuffer SortConstants : register(b2)
{
    uint SortJ;
    uint SortK;
    uint SortCount;
    uint _SortPadding;
};

[numthreads(256, 1, 1)]
void main(uint3 dtid : SV_DispatchThreadID)
{
    uint i = dtid.x;

    if (SortK == 0)
    {
        if (i >= ParticleCount)
            return;

        Particle p = PrevParticles[i];
        HashEntry entry;
        entry.Hash = (p.Age >= 0.0f) ? CellHashFromPosition(p.Position) : 0xffffffffu;
        entry.ParticleIndex = i;
        HashEntries[i] = entry;
        return;
    }

    if (i >= SortCount)
        return;

    uint ixj = i ^ SortJ;
    if (ixj <= i || ixj >= SortCount || i >= ParticleCount || ixj >= ParticleCount)
        return;

    HashEntry a = HashEntries[i];
    HashEntry b = HashEntries[ixj];
    bool ascending = (i & SortK) == 0;
    bool swapEntries = ascending ? (a.Hash > b.Hash) : (a.Hash < b.Hash);

    if (swapEntries)
    {
        HashEntries[i] = b;
        HashEntries[ixj] = a;
    }
}
