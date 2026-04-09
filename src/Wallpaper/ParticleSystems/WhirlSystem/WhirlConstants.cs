
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct WhirlConstants
{
    public float LifeTime;
    public uint ParticleCount;
    public Vector2 CenterPosition;
    public float SpawnRate;
}