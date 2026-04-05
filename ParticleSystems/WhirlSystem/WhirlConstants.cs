
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct WhirlConstants
{
    public Matrix4x4 ViewMatrix;
    public float DeltaTime;
    public uint FrameIndex;
    public float LifeTime;
    public uint ParticleCount;
    public float SpawnRate;
}