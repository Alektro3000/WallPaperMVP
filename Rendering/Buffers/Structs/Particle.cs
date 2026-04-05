using Vortice.Mathematics;
using System.Runtime.InteropServices;
using System.Numerics;


[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
    public Vector3 Position; 
    public float Size;
    public Vector3 Velocity; 
    public float Age;
    public Vector4 Color;
}

