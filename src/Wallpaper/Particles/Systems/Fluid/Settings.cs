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



    [UiLabel("Particle Size")]
    [UiRange(0.001f, 0.08f, 0.001f)]
    public float Size = 0.02f;


    [UiLabel("Cell Size")]
    [UiRange(0.001f, 0.8f, 0.001f)]
    public float GridSize = 0.1f;

    [UiLabel("Influence Radius")]
    [UiRange(0.001f, 0.8f, 0.001f)]
    public float InfluenceRadius = 0.1f;

    [UiLabel("Rest Density")]
    [UiRange(0f, 200f, 0.01f)]
    public float RestDensity = 3f;

    [UiLabel("Pressure")]
    [UiRange(0f, 2000f, 0.01f)]
    public float Pressure = 10f;


    

    [UiLabel("Viscosity")]
    [UiRange(0f, 1000f, 0.01f)]
    public float Viscosity = 4f;

    [UiLabel("Gravity")]
    [UiRange(-10f, 10f, 0.01f)]
    public float Gravity = -1f;

    [UiLabel("Windows Force")]
    [UiRange(0f, 200f, 0.1f)]
    public float WindowsForce = 20f;
    
    [UiLabel("Windows Offset")]
    [UiRange(-200f, 200f, 0.1f)]
    public float WindowsOffset = 0f;


    [UiLabel("Soft Boundary Scale")]
    [UiRange(0f, 1f, 0.001f)]
    public float SoftBoundaryScale = 1;

    
    [UiLabel("Boundary Hardness")]
    [UiRange(0.01f, 1000f, 0.1f)]
    public float BoundaryHardness = 100f;
    
    [UiLabel("Boundary Force")]
    [UiRange(0f, 200f, 0.1f)]
    public float BoundaryForce = 200f;

    [UiLabel("Separatation Radius")]
    [UiRange(0.0001f, 0.1f, 0.0001f)]
    public float SeparatationRadius = 0.001f;
    
    [UiLabel("Separatation Strength")]
    [UiRange(0f, 2000f, 0.1f)]
    public float SeparatationStrength = 20f;


    [UiLabel("Density Debug")]
    [UiRange(0f, 1f, 1f)]
    public float DensityDebug = 0f;

    [UiLabel("Density Debug Min")]
    [UiRange(0f, 200f, 0.1f)]
    public float DensityDebugMin = 0f;
    
    [UiLabel("Density Debug Max")]
    [UiRange(0f, 200f, 0.1f)]
    public float DensityDebugMax = 2f;

    


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
