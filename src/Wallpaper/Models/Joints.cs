using System.Numerics;
using System.Runtime.CompilerServices;

[InlineArray(640)]
struct JointsStaticBufferHelper
{
    public Matrix4x4 TransformMatrix4X4;
}

struct JointsStaticBuffer
{
    public JointsStaticBufferHelper buffer;
}