
using System.Numerics;
using System.Runtime.InteropServices;


namespace ParticleSystems.Whirl;

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public uint ParticleCount;
    public Vector3 _padding;
    public GpuSettings Settings = new GpuSettings();

    public Constants()
    {
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct GpuSettings
{

    [UiLabel("Begin Color")]
    [UiColor]
    public Vector3 BeginColor = new Vector3(0.4f,0.18f,1f);
    
    [UiLabel("Spawn Rate")]
    [UiRange(0f, 10000f, 1f)]
    public float SpawnRate = 150f;
    
    [UiLabel("End Color")]
    [UiColor]
    public Vector3 EndColor = new Vector3(0.4f,1.08f,1f);
    
    [UiLabel("Life Time")]
    [UiRange(0.01f, 100f, 0.01f)]
    public float LifeTime = 3f;

    [UiLabel("Center Position")]
    [UiVector2(
        minX: -3.2f, maxX: 3.2f, stepX: 0.01f,
        minY: -1.2f, maxY: 1.2f, stepY: 0.01f,
        xLabel: "X",
        yLabel: "Y")]
    public Vector2 CenterPosition = new Vector2(0f,0.2f);
    
    [UiLabel("Speed")]
    [UiRange(-10f, 10f, 0.01f)]
    public float Speed = 0.2f;

    
    [UiLabel("Tangent")]
    [UiRange(-100f, 100f, 0.1f)]
    public float Tangent = 1;
    
    [UiLabel("Radial")]
    [UiRange(-10f, 10f, 0.01f)]
    public float Radial = 0.1f;

    
    [UiLabel("Size")]
    [UiRange(0.001f, 0.2f, 0.001f)]
    public float Size = 0.06f;
    
    [UiLabel("Init Region")]
    [UiRange(0f, 100f, 0.001f)]
    public float InitRegion = 3f;
    
    [UiLabel("Init Offset")]
    [UiRange(0f, 1f, 0.01f)]
    public float InitOffset = 0.4f;

    public GpuSettings()
    {
    }
}


public struct Settings : ISettings
{
    public CommonInitSettings initSettings = new CommonInitSettings(2048);
    public GpuSettings gpuSettings = new GpuSettings();
    public Settings()
    {
        
    }

}