
using System.Numerics;
using System.Runtime.InteropServices;

namespace TextSystem;

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public uint ParticleCount;
    public Vector3 padding;
    public Settings Settings;
}

[StructLayout(LayoutKind.Sequential)]
public struct Settings
{
    
    [UiLabel("Begin Color")]
    [UiColor]
    public Vector3 BeginColor = new Vector3(0.4f,0.18f,1f);
    
    
    [UiLabel("Spawn Rate")]
    [UiRange(0.1f, 30f, 0.1f)]
    public float LifeTime = 3f;

    [UiLabel("End Color")]
    [UiColor]
    public Vector3 EndColor = new Vector3(0.4f,1.08f,1f);
    
    [UiLabel("Spawn Rate")]
    [UiRange(0f, 10000f, 1f)]
    public float SpawnRate = 1000f;

    [UiLabel("Size")]
    [UiRange(0.001f, 0.2f, 0.001f)]
    public float Size = 0.01f;
    
    [UiLabel("Speed")]
    [UiRange(-1f, 1f, 0.001f)]
    public float Speed = 0.01f;
    
    [UiLabel("Init Region")]
    [UiRange(0f, 100f, 0.001f)]
    public float InitRegion = 10f;
    
    [UiLabel("Init Offset")]
    [UiRange(0f, 1f, 0.01f)]
    public float InitOffset = 0.5f;

    public Settings()
    {
    }
}