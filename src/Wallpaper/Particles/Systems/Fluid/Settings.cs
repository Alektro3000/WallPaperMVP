using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;

namespace Particles.Systems.Fluid;

[StructLayout(LayoutKind.Sequential)]
public struct GpuSettings
{
    [UiColor]
    public Vector3 BeginColor = new(0.1f, 0.55f, 1.0f);



    [UiRange(0.001f, 0.08f, 0.001f)]
    public float Size = 0.02f;


    [UiRange(0.001f, 0.8f, 0.001f)]
    public float GridSize = 0.1f;

    [UiRange(0.001f, 0.8f, 0.001f)]
    public float InfluenceRadius = 0.1f;

    [UiRange(0f, 200f, 0.01f)]
    public float RestDensity = 3f;

    [UiRange(0f, 2000f, 0.01f)]
    public float Pressure = 10f;


    

    [UiRange(0f, 1000f, 0.01f)]
    public float Viscosity = 4f;

    [UiRange(-10f, 10f, 0.01f)]
    public float Gravity = -1f;
    

    [UiRange(0f, 200f, 0.1f)]
    public float WindowsForce = 20f;
    
    [UiRange(-200f, 200f, 0.1f)]
    public float WindowsOffset = 0f;
    
    [UiRange(-200f, 200f, 0.1f)]
    public float WindowsVelocityScale = 0f;


    [UiRange(0f, 1f, 0.001f)]
    public float SoftBoundaryScale = 1;

    
    [UiRange(0.01f, 1000f, 0.1f)]
    public float BoundaryHardness = 100f;
    
    [UiRange(0f, 200f, 0.1f)]
    public float BoundaryForce = 200f;

    [UiRange(0.0001f, 0.1f, 0.0001f)]
    public float SeparatationRadius = 0.001f;
    
    [UiRange(0f, 2000f, 0.1f)]
    public float SeparatationStrength = 20f;

    [UiRange(-500f, 500f, 0.1f)]
    public float MouseStrength = 80f;

    [UiRange(0.01f, 2f, 0.01f)]
    public float MouseRadius = 0.35f;

    [UiRange(0f, 1f, 1f)]
    public float DensityDebug = 0f;

    [UiRange(0f, 200f, 0.1f)]
    public float DensityDebugMin = 0f;
    
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
