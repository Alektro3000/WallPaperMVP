

using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;

namespace Particles.Shared.Field;

[StructLayout(LayoutKind.Sequential)]
public struct DebugSettings
{
    
    public Vector2 Center = new Vector2(0.1f,0.1f);
    public Vector2 Size = new Vector2(0.1f,0.1f);

    [UiColor]
    public Vector3 minColor = new Vector3(0,0,0);
    public float min = -10f;

    [UiColor]
    public Vector3 maxColor  = new Vector3(1,1,1);
    public float max = 10f;
    
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
    [UiLabel("Density Debug")]
    [UiRange(0f, 1f, 1f)]
    public float IsDebugModeEnabled = 0;

    public DebugSettings gpuSettings = new DebugSettings();

    public Settings()
    {
    }
}