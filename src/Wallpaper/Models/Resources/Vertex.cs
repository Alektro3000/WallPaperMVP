using System.Numerics;
using System.Runtime.InteropServices;

namespace Models;
[StructLayout(LayoutKind.Sequential)]
public struct StaticVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector4 Tangent;
    public Vector2 UV;
}
