
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public Matrix4x4 ViewMatrix;
    public Vector4 TintColor;
    public uint ParticleCount;
    public float DeltaTime;
    public Vector2 MousePos;
    public float LifeTime;
    public float SpawnRate;
    public uint FrameIndex;
}