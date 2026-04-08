using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct MouseConstants
{
    public float LifeTime;
    public uint ParticleCount;
    public Vector2 mousePos;

    public Vector2 mousePosPrev;
    public float SpawnRate;
    public float SpawnRatePerUnit;

    public Vector3 Color;
    public float Size;
    
    public Vector2 GridSize;
    public float Velocity;
}