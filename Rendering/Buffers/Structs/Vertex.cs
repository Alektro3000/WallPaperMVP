

using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct QuadVertex
{
    public Vector2 LocalOffset; // -0.5..0.5 quad corners
    public Vector2 UV;
}