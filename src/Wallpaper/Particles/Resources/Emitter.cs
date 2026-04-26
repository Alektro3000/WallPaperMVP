using System.Numerics;
using System.Runtime.InteropServices;

namespace Particles.Resources;

[StructLayout(LayoutKind.Sequential)]
public struct Emitter
{
    public uint SpawnCountThisFrame;
    public uint ConsumedSpawns;
    public uint AccumulatedSpawns;
    public uint AliveCount = 0;
    public uint TotalCount = 0;
    public uint AliveCountCheck = 0;

    public Emitter()
    {
    }
}