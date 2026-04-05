using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct MouseConstants
{
    public Matrix4x4 ViewMatrix;
    public float DeltaTime;
    public uint FrameIndex;
    public float LifeTime;
    public uint ParticleCount;
    public Vector2 mousePos;
    public Vector2 mousePosPrev;
    public float SpawnRate;
    public float SpawnRatePerUnit;
    public Vector2 GridSize;
    public Vector3 Color;
    public float Size;
    public float Velocity;
}