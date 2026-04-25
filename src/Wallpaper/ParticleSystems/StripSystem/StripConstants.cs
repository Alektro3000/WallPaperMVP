
using System.Numerics;
using System.Runtime.InteropServices;

namespace ParticleSystems.Strip;

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StripDescription
    {
        public Vector2 position;
        public Vector2 size;
        public StripDescription(float positionX, float positionY, float sizeX, float sizeY)
        {
            position = new Vector2(positionX, positionY);
            size = new Vector2(sizeX, sizeY);
        }
    }
    public uint ParticleCount;
    private Vector3 _padding;

    public StripDescription strip0;
    public StripDescription strip1;
    public StripDescription strip2;
    public StripDescription strip3;
    public StripDescription strip4;
    public GpuSettings Settings;
}

public struct GpuSettings
{
    [UiLabel("Color")]
    [UiColor(normalized: true)]
    public Vector3 Color = new Vector3(0.2f, 0.9f, 1f);
    [UiLabel("Spawn Rate")]
    [UiRange(0f, 5000f, 1f)]
    public float SpawnRate = 500f;
    
    [UiLabel("Acceleration")]
    [UiRange(-20f, 20f, 0.1f)]
    public float Acceleration = 1;

    [UiLabel("Size")]
    [UiRange(0.001f, 0.2f, 0.001f)]
    public float Size = 0.04f;
    
    [UiLabel("Grid Size")]
    [UiVector2(
        minX: 0.001f, maxX: 0.2f, stepX: 0.001f,
        minY: 0.001f, maxY: 0.2f, stepY: 0.001f,
        xLabel: "Width",
        yLabel: "Height")]
    public Vector2 GridSize = new Vector2(0.01f, 0.01f);
    
    [UiLabel("LifeTime")]
    [UiRange(0f, 20f, 0.01f)]
    public float LifeTime = 3f;

    public GpuSettings()
    {
    }
}


public struct Settings : ISettings
{
    public CommonInitSettings initSettings = new CommonInitSettings(4096);
    public GpuSettings gpuSettings = new GpuSettings();
    public Settings()
    {
        
    }

}