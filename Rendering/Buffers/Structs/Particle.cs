using Vortice.Mathematics;
using System.Runtime.InteropServices;
using System.Numerics;


[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
    public Vector3 Position; 
    public Vector3 Velocity; 
    public Vector3 Color; 
    public float Age;
}

