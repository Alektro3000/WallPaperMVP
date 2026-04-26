
using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;

namespace Particles.Systems.Text;


[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public uint ParticleCount;
    public Vector3 padding;
    public GpuSettings Settings;
}

[StructLayout(LayoutKind.Sequential)]
public struct GpuSettings
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

    public GpuSettings()
    {
    }
}

public struct InitSettings
{
    [UiLabel("Max Particle Amount")]
    [UiRange(0f, 65536f, 1f)]
    public float MaxParticleAmount = 4096;


    public string Text = "Встречая страх, создавай будущее";

    [UiLabel("Resolution")]
    [UiVector2(
        minX: 1f, maxX: 4096f, stepX: 1f,
        minY: 1f, maxY: 4096f, stepY: 1f,
        xLabel: "Width",
        yLabel: "Height")]
    public Vector2 Resolution = new Vector2(900, 120);
    
    [UiLabel("Text Size")]
    [UiRange(0f, 300f, 0.1f)]
    public float TextSize = 24;
    public string Font = "Times New Roman";
    
    [UiLabel("Text Size")]
    [UiRange(0f, 1f, 0.000001f)]
    public float PixelSize =  0.0028f;
    
    [UiLabel("Text Position")]
    [UiVector2(
        minX: -10f, maxX: 10f, stepX: 0.001f,
        minY: -10f, maxY: 10f, stepY: 0.001f,
        xLabel: "Width",
        yLabel: "Height")]
    public Vector2 CenterPos = new Vector2(0f, -0.4f);

    public InitSettings()
    {
    }

}

public struct Settings : ISettings
{
    public InitSettings initSettings = new InitSettings();
    public GpuSettings gpuSettings = new GpuSettings();
    public Settings()
    {
        
    }

}