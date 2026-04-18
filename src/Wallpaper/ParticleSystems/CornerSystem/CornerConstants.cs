using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct CornerConstants
{
    public uint ParticleCount;
    private Vector3 _padding;
    public CornerSettings settings;
}

[StructLayout(LayoutKind.Sequential)]
public struct CornerSettings
{
    [UiLabel("Color")]
    [UiColor]
    public Vector3 Color = new Vector3(0.2f, 0.9f, 1f);

    [UiLabel("Spawn rate")]
    [UiRange(0.001f, 0.2f, 0.001f)]
    public float SpawnRate = 70f;

    [UiLabel("Spawn offset")]
    [UiVector2(
        minX: -2f, maxX: 2f, stepX: 0.01f,
        minY: -1.2f, maxY: 1.2f, stepY: 0.01f,
        xLabel: "X",
        yLabel: "Y")]
    public Vector2 SpawnPosition = new Vector2(0.03f);
    [UiLabel("Spawn radius")]
    [UiVector2(
        minX: -2f, maxX: 2f, stepX: 0.001f,
        minY: -1.2f, maxY: 1.2f, stepY: 0.001f,
        xLabel: "X",
        yLabel: "Y")]
    public Vector2 SpawnDistribution = new Vector2(0.4f);

    [UiLabel("Remove distance")]
    [UiVector2(
        minX: -2f, maxX: 2f, stepX: 0.001f,
        minY: -1.2f, maxY: 1.2f, stepY: 0.001f,
        xLabel: "X",
        yLabel: "Y")]
    public Vector2 RemoveBox = new Vector2(0.05f);
    
    [UiLabel("Size")]
    [UiRange(0.001f, 0.2f, 0.001f)]
    public float Size = 0.05f;
    
    [UiLabel("Life time")]
    [UiRange(0.1f, 12f, 0.1f)]
    public float LifeTime = 6f;

    [UiLabel("Velocity")]
    [UiRange(0.01f, 100f, 0.01f)]
    public float Velocity = 1f;

    public CornerSettings()
    {
    }
}