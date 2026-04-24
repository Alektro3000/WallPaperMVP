using System.Numerics;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Sequential)]
public struct MouseSettings
{
    [UiLabel("Color")]
    [UiColor(normalized: true)]
    public Vector3 Color = new Vector3(0.9f, 0.2f, 1f);

    [UiLabel("Size")]
    [UiRange(0.001f, 0.2f, 0.001f)]
    public float Size = 0.016f;



    [UiLabel("Grid Size")]
    [UiVector2(
        minX: 0.001f, maxX: 0.2f, stepX: 0.001f,
        minY: 0.001f, maxY: 0.2f, stepY: 0.001f,
        xLabel: "Width",
        yLabel: "Height")]
    public Vector2 GridSize;

    [UiLabel("Radial Speed")]
    [UiRange(-5f, 5f, 0.05f)]
    public float Radial = 0.5f;
    
    [UiLabel("Tangent Speed")]
    [UiRange(-5f, 5f, 0.05f)]
    public float Tangent = 0.5f;



    [UiLabel("Life Time")]
    [UiRange(0.1f, 10f, 0.1f)]
    public float LifeTime = 1f;

    [UiLabel("Spawn Rate")]
    [UiRange(0f, 5000f, 10f)]
    public float SpawnRate = 300f;

    [UiLabel("Spawn Rate Per Unit")]
    [UiRange(0f, 1000f, 5f)]
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
    [UiRange(0f, 10f, 0.01f)]
    public float StationaryVelocity = 0.1f;



    
    [UiLabel("Velocity Fallof")]
    [UiRange(-20f, 20f, 1f)]
    public float VelocityFallof = 1f;

    [UiLabel("Wave Length")]
    [UiRange(-20f, 20f, 0.001f)]
    public float WaveLength = 0.1f;

    public MouseSettings(float size = 0.016f) : this()
    {
        Size = size;
        GridSize = new Vector2(size);
    }
}