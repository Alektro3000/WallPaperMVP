using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;

namespace Particles.Systems.Fluid;

[StructLayout(LayoutKind.Sequential)]
public struct GpuSettings
{
    [UiLabel("Begin Color")]
    [UiColor]
    public Vector3 BeginColor = new(0.1f, 0.55f, 1.0f);

    [UiLabel("End Color")]
    [UiColor]
    public Vector3 EndColor = new(0.85f, 1.0f, 1.0f);

    [UiLabel("Spawn Rate")]
    [UiRange(0f, 20000f, 1f)]
    public float SpawnRate = 900f;

    [UiLabel("Life Time")]
    [UiRange(0.1f, 100f, 0.1f)]
    public float LifeTime = 20f;

    [UiLabel("Particle Size")]
    [UiRange(0.001f, 0.08f, 0.001f)]
    public float Size = 0.025f;

    [UiLabel("Cell Size")]
    [UiVector2(0.01f, 0.5f, 0.001f, 0.01f, 0.5f, 0.001f, "X", "Y")]
    public Vector2 GridSize = new(0.055f, 0.055f);

    [UiLabel("Emitter")]
    [UiVector2(-3.2f, 3.2f, 0.01f, -1.8f, 1.8f, 0.01f, "X", "Y")]
    public Vector2 EmitterPosition = new(0f, 0.7f);

    [UiLabel("Emitter Radius")]
    [UiRange(0.01f, 1f, 0.01f)]
    public float EmitterRadius = 0.25f;

    [UiLabel("Initial Velocity")]
    [UiRange(0f, 5f, 0.01f)]
    public float InitialVelocity = 0.6f;

    [UiLabel("Rest Density")]
    [UiRange(0f, 200f, 0.1f)]
    public float RestDensity = 35f;

    [UiLabel("Pressure")]
    [UiRange(0f, 2000f, 0.01f)]
    public float Pressure = 2.2f;

    [UiLabel("Viscosity")]
    [UiRange(0f, 1000f, 0.01f)]
    public float Viscosity = 0.8f;

    [UiLabel("Gravity")]
    [UiRange(-10f, 10f, 0.01f)]
    public float Gravity = -1.2f;

    [UiLabel("Density Debug")]
    [UiRange(0f, 1f, 1f)]
    public float DensityDebug = 0f;

    
    [UiLabel("Density Debug Min")]
    [UiRange(0f, 200f, 0.1f)]
    public float DensityDebugMin = 0f;
    
    [UiLabel("Density Debug Max")]
    [UiRange(0f, 200f, 0.1f)]
    public float DensityDebugMax = 2f;

    
    [UiLabel("Separatation Radius")]
    [UiRange(0.0001f, 0.01f, 0.0001f)]
    public float SeparatationRadius = 0001f;
    
    [UiLabel("Separatation Strength")]
    [UiRange(0f, 200f, 0.1f)]
    public float SeparatationStrength = 2f;

    public GpuSettings()
    {
    }
}

public struct Settings : ISettings
{
    public CommonInitSettings initSettings = new(4096);
    public GpuSettings gpuSettings = new();

    public Settings()
    {
    }
}
