using System.Numerics;
using System.Runtime.CompilerServices;

[InlineArray(512+128)]
struct JointsStaticBufferHelper
{
    public Matrix4x4 matrix4X4;
}

struct JointsStaticBuffer
{
    public JointsStaticBufferHelper buffer;
}