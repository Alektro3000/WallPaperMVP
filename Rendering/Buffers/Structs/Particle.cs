using Vortice.Mathematics;
using System.Runtime.InteropServices;
using System.Numerics;


[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
    public Vector4 Color;
    public Vector2 Position; 
    public Vector2 Velocity; 
    public float Age;
    public float Size;
    public Vector4 CustomData;
}

