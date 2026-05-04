

using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;

namespace Particles.Shared.Field;

[StructLayout(LayoutKind.Sequential)]
public struct FieldGpuSettings
{
    public float EdgeInfluence = 0.02f;
    public float WindowInfluence = 0.005f;
    public float InfluenceRadius = 5.0f;
    public float Padding;
    public FieldGpuSettings()
    {
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct DebugSettings
{
    
    public Vector2 Center = new Vector2(0.1f,0.1f);
    public Vector2 Size = new Vector2(0.1f,0.1f);

    [UiColor]
    public Vector3 MinColor = new Vector3(0,0,0);
    public float MinDisplayedValue = -10f;

    [UiColor]
    public Vector3 MaxColor  = new Vector3(1,1,1);
    public float MaxDisplayedValue = 10f;
    
    [UiRange(0f, 3f, 1f)]
    public float MaskId = 0f;
    [UiRange(0f, 1f, 1f)]
    public float ShowVelocity = 1f;
    Vector2 _padding;

    public DebugSettings()
    {
    }
}

public struct Settings : ISettings
{
    public FieldWindowSettings fieldWindowSettings = new FieldWindowSettings();
    public FieldGpuSettings fieldSettings = new FieldGpuSettings();

    [UiLabel("Density Debug")]
    [UiRange(0f, 1f, 1f)]
    public float IsDebugModeEnabled = 0;

    public DebugSettings debugSettings = new DebugSettings();

    public Settings()
    {
    }
}

public struct FieldWindowSettings
{
    public float BorderExtendFactor = 0.5f;
    public float BorderTransitionDistance = 5f;

    public FieldWindowSettings()
    {
    }
}