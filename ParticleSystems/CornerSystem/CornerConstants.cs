using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct CornerConstants
{
    public Matrix4x4 ViewMatrix;
    public float DeltaTime;
    public uint FrameIndex;
    public float LifeTime;
    public uint ParticleCount;
    public Vector3 Color;
    public float SpawnRate;
    public Vector2 SpawnPosition;
    public Vector2 SpawnDistribution;
    public Vector2 RemoveBox;
    public float Size;
    public float Velocity;
}