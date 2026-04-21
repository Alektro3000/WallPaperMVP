using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct Emitter
{
    public uint SpawnCountThisFrame;
    public uint ConsumedSpawns;
    public uint AccumulatedSpawns;
    public uint AliveCount = 0;

    public Emitter()
    {
    }
}