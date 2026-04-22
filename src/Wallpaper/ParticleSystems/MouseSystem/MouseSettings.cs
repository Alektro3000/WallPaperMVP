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

    [UiLabel("Velocity")]
    [UiRange(-5f, 5f, 0.05f)]
    public float Velocity = 0.5f;

    [UiLabel("Life Time")]
    [UiRange(0.1f, 10f, 0.1f)]
    public float LifeTime = 1f;

    [UiLabel("Spawn Rate")]
    [UiRange(0f, 5000f, 10f)]
    public float SpawnRate = 000f;

    [UiLabel("Spawn Rate Per Unit")]
    [UiRange(0f, 1000f, 5f)]
    public float SpawnRatePerUnit = 100f;

    [UiLabel("Initial Speed")]
    [UiRange(-200f, 200f, 1f)]
    public float InitSpeed = 40f;

    public MouseSettings(float size = 0.016f) : this()
    {
        Size = size;
        GridSize = new Vector2(size);
    }
}