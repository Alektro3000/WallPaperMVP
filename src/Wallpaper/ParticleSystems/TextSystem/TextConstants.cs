
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct TextConstants
{
    public float LifeTime;
    public uint ParticleCount;
    public float SpawnRate;
}