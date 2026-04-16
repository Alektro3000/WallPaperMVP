
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct TextConstants
{
    public uint ParticleCount;
    public Vector3 padding;
    public TextSettings Settings;
}

[StructLayout(LayoutKind.Sequential)]
public struct TextSettings
{
    public float LifeTime = 3f;
    public float SpawnRate = 1000f;
    public float Size = 0.01f;
    public float Speed = 0.01f;
    
    
    public float InitRegion = 10;
    public float InitOffset = 0.5f;

    public TextSettings()
    {
    }
}