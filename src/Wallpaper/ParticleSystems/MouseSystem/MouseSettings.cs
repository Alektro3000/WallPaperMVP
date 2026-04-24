using System.Numerics;
using System.Runtime.InteropServices;

namespace MouseSystem;

[StructLayout(LayoutKind.Sequential)]
public struct GpuSettings
{
    [UiLabel("Color")]
    [UiColor(normalized: true)]
    public Vector3 Color = new Vector3(0.9f, 0.2f, 1f);

    [UiLabel("Radial Speed")]
    [UiRange(-5f, 5f, 0.05f)]
    public float Radial = 0.5f;



    [UiLabel("Life Time")]
    [UiRange(0.1f, 10f, 0.1f)]
    public float LifeTime = 1f;

    [UiLabel("Spawn Rate")]
    [UiRange(0f, 50000f, 10f)]
    public float SpawnRate = 300f;

    [UiLabel("Spawn Rate Per Unit")]
    [UiRange(0f, 10000f, 5f)]
    public float SpawnRatePerUnit = 100f;

    [UiLabel("Initial Speed")]
    [UiRange(-20f, 20f, 0.01f)]
    public float InitSpeed = 1f;



    [UiLabel("Stationary Lerp Start")]
    [UiRange(0f, 100f, 0.1f)]
    public float StationaryLerpStart = 0.2f;

    [UiLabel("Stationary Lerp End")]
    [UiRange(0f, 100f, 0.1f)]
    public float StationaryLerpEnd = 3;

    [UiLabel("Offset Lerp End")]
    [UiRange(0f, 100f, 0.1f)]
    public float OffsetLerpStart = 30;

    [UiLabel("Offset Lerp End")]
    [UiRange(0f, 100f, 0.1f)]
    public float OffsetLerpEnd = 10;



    [UiLabel("Strip Width")]
    [UiRange(-1f, 1f, 0.001f)]
    public float StripWidth = 0.05f;
    
    [UiLabel("Spark Percent")]
    [UiRange(0f, 1f, 0.01f)]
    public float SparkPercent = 0.05f;

    [UiLabel("Stationary Velocity")]
    [UiRange(-10f, 10f, 0.01f)]
    public float StationaryVelocity = 0.1f;


    public GpuSettings()
    {
        
    }
}


public struct CpuSettings
{
    [UiLabel("Size")]
    [UiRange(1f, 1080f, 1f)]
    public float Size = 13f;

    [UiLabel("Grid Size")]
    [UiVector2(
        minX: 1f, maxX: 1080f, stepX: 1f,
        minY: 1f, maxY: 1080f, stepY: 1f,
        xLabel: "Width",
        yLabel: "Height")]
    public Vector2 GridSize = new Vector2(13, 13);

    [UiLabel("Velocity Fallof")]
    [UiRange(-20f, 20f, 0.01f)]
    public float VelocityFallof = 1f;

    [UiLabel("Wave Length")]
    [UiRange(-20f, 20f, 0.01f)]
    public float WaveLength = 1f;

    public CpuSettings()
    {
    }
}

public struct Settings
{
    public GpuSettings gpuSettings = new GpuSettings();
    public CpuSettings cpuSettings = new CpuSettings();
    public Settings()
    {
        
    }
}