using Vortice.Mathematics;
using System.Runtime.InteropServices;
using System.Numerics;
// Define a struct that matches the vertex shader's input layout.
// The position is a 3-component vector of floats.

[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
    public Vector3 Position; 
    public Vector3 Velocity; 
    public Vector3 Color; 
}

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public Vector4 TintColor;
    public uint particleCount;
    public float DeltaTime;
    public Vector2 MousePos;
}

[StructLayout(LayoutKind.Sequential)]
public struct QuadVertex
{
    public Vector2 LocalOffset; // -0.5..0.5 quad corners
    public Vector2 UV;
}