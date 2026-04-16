
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct WhirlConstants
{
    public uint ParticleCount;
    public Vector3 _padding;
    public WhirlSettings WhirlSettings = new WhirlSettings();

    public WhirlConstants()
    {
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct WhirlSettings
{
    public Vector2 CenterPosition = new Vector2(0f,0.2f);
    public float LifeTime = 3f;
    public float SpawnRate = 150f;
    
    public float Speed = 0.2f;
    public float Tangent = 1;
    public float Radial = 0.1f;
    public float Size = 0.06f;
    
    public float InitRegion = 3f;
    public float InitOffset = 0.4f;

    public WhirlSettings()
    {
    }
}