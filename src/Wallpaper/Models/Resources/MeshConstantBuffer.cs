
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


[InlineArray(8)]
public struct LightConstantBufferHelper
{
    public LightConstant LightConstant;
}

[StructLayout(LayoutKind.Sequential)]
public struct MeshConstantBuffer
{
    public Matrix4x4 inverseModelTransform;
    public Matrix4x4 modelTransform;
}
