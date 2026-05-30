using System.Numerics;
using System.Runtime.CompilerServices;

[InlineArray(1024)]
public struct JointsStaticBufferHelper
{
    public Matrix4x4 TransformMatrix4X4;
}

public struct JointsStaticBuffer
{
    public JointsStaticBufferHelper buffer;
}