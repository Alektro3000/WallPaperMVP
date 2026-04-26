using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;

namespace Particles.Systems.Mouse;

[StructLayout(LayoutKind.Sequential)]
public struct GpuSettings
{
    [UiLabel("Begin Color")]
    [UiColor]
    public Vector3 BeginColor = new Vector3(0.9f, 0.2f, 1f);
    float Padding;

    [UiLabel("End Color")]
    [UiColor]
    public Vector3 EndColor = new Vector3(0.2f, 0.2f, 1f);

    [UiLabel("Mouse Velocity Influence")]
    [UiRange(-20f, 20f, 0.01f)]
    public float MouseVelocityInfluence = 1f;
    


    [UiLabel("Life Time")]
    [UiRange(0.1f, 10f, 0.1f)]
    public float LifeTime = 1f;

    [UiLabel("Spawn Rate Per Second")]
    [UiRange(0f, 50000f, 10f)]
    public float SpawnRate = 1000f;

    [UiLabel("Spawn Rate Per Unit")]
    [UiRange(0f, 10000f, 5f)]
    public float SpawnRatePerUnit = 300f;

    [UiLabel("Spark Percent")]
    [UiRange(0f, 1f, 0.01f)]
    public float SparkPercent = 0.05f;



    [UiLabel("Trail Shrink Start Speed")]
    [UiRange(0f, 100f, 0.1f)]
    public float OffsetLerpStart = 30;

    [UiLabel("Trail Shrink End Speed")]
    [UiRange(0f, 100f, 0.1f)]
    public float OffsetLerpEnd = 10;

    [UiLabel("Trail Size")]
    [UiRange(-1f, 1f, 0.001f)]
    public float TrailSize = 0.05f;
    
    float Padding2;



    [UiLabel("Stationary Lerp Start")]
    [UiRange(0f, 100f, 0.1f)]
    public float StationaryLerpStart = 0.2f;

    [UiLabel("Stationary Lerp End")]
    [UiRange(0f, 100f, 0.1f)]
    public float StationaryLerpEnd = 3;

    [UiLabel("Stationary Spawn Velocity")]
    [UiRange(-10f, 10f, 0.01f)]
    public float StationaryVelocity = 0.1f;

    [UiLabel("Stationary Spawn Radius")]
    [UiRange(-1f, 1f, 0.001f)]
    public float StationarySpawnRadius = 0.01f;



    public GpuSettings()
    {
        
    }
}


public struct CpuSettings
{
    [UiLabel("Particle Size")]
    [UiRange(1f, 1080f, 1f)]
    public float Size = 4f;

    [UiLabel("Grid Size")]
    [UiVector2(
        minX: 1f, maxX: 1080f, stepX: 1f,
        minY: 1f, maxY: 1080f, stepY: 1f,
        xLabel: "Width",
        yLabel: "Height")]
    public Vector2 GridSize = new Vector2(6, 6);

    [UiLabel("Velocity Fallof")]
    [UiRange(-20f, 20f, 0.01f)]
    public float VelocityFallof = 1f;

    [UiLabel("Wave Length")]
    [UiRange(0.01f, 20f, 0.01f)]
    public float WaveLength = 1f;

    public CpuSettings()
    {
    }
}

public struct Settings : ISettings
{
    public CommonInitSettings initSettings = new CommonInitSettings(65536);
    public GpuSettings gpuSettings = new GpuSettings();
    public CpuSettings cpuSettings = new CpuSettings();
    public Settings()
    {
        
    }

    public CommonInitSettings GetInitSettings() => initSettings;
}