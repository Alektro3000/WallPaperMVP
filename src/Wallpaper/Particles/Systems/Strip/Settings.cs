
using System.Numerics;
using Particles.Settings;
using System.Runtime.InteropServices;

namespace Particles.Systems.Strip;

[StructLayout(LayoutKind.Sequential)]
public struct GpuSettings
{
    [UiLabel("Color")]
    [UiColor(normalized: true)]
    public Vector3 Color = new Vector3(0.2f, 0.9f, 1f);
    [UiLabel("Spawn Rate")]
    [UiRange(0f, 5000f, 1f)]
    public float SpawnRate = 500f;
    
    [UiLabel("Acceleration")]
    [UiRange(-20f, 20f, 0.001f)]
    public float Acceleration = 0.01f;

    [UiLabel("LifeTime")]
    [UiRange(0f, 20f, 0.01f)]
    public float LifeTime = 3f;

    public GpuSettings()
    {
    }
}

public struct CpuSettings
{
    [UiLabel("Particle Size")]
    [UiRange(1f, 1080f, 1f)]
    public float Size = 19f;

    [UiLabel("Grid Size")]
    [UiVector2(
        minX: 1f, maxX: 1080f, stepX: 1f,
        minY: 1f, maxY: 1080f, stepY: 1f,
        xLabel: "Width",
        yLabel: "Height")]
    public Vector2 GridSize = new Vector2(6, 6);

    
    [UiLabel("Base Strip Height")]
    [UiRange(0.01f, 2f, 0.01f)]
    public float StripHeightBase = 1f;
    
    [UiLabel("Base Strip Distance")]
    [UiRange(0.01f, 2f, 0.01f)]
    public float StripOffset = 1.06f;
    
    [UiLabel("Distance between Strips")]
    [UiRange(0.01f, 2f, 0.01f)]
    public float StripDistance = 0.12f;

    
    [UiLabel("Strip Height Scaling")]
    [UiRange(0.01f, 2f, 0.01f)]
    public float StripHeightScaling = 0.1f;
    
    [UiLabel("Strip Height Offset")]
    [UiRange(0.01f, 2f, 0.01f)]
    public float StripHeightOffset = 0.1f;

    public CpuSettings()
    {
    }

}

public struct Settings : ISettings
{
    public CommonInitSettings initSettings = new CommonInitSettings(4096);
    public GpuSettings gpuSettings = new GpuSettings();
    public CpuSettings cpuSettings = new CpuSettings();
    public Settings()
    {
        
    }

}