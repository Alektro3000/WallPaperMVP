using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct MouseConstants
{
    public Vector2 mousePos;
    public Vector2 mousePosPrev;

    public uint ParticleCount;
    public Vector3 padding;

    public MouseSettings mouseSettings;

}