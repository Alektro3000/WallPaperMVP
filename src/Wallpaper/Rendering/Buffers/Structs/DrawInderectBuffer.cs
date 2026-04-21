using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct DrawIndexedArguments
{
    public uint IndexCountPerInstance;
    public uint InstanceCount;
    public uint StartIndexLocation;
    public int BaseVertexLocation;
    public uint StartInstanceLocation;

    public DrawIndexedArguments(uint indexCountPerInstance, uint instanceCount)
    {
        IndexCountPerInstance = indexCountPerInstance;
        InstanceCount = instanceCount;
        StartIndexLocation = 0;
        BaseVertexLocation = 0;
        StartInstanceLocation = 0;
    }
}