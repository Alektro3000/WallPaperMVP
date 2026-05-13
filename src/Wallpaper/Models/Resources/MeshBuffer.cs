
using Vortice.Direct3D12;

public class MeshBuffer : IDisposable
{
    public ID3D12Resource meshBuffer;

    public MeshBuffer(VertexIndexRegistry vertexIndexRegistry, InitContext context)
    {
        meshBuffer = vertexIndexRegistry.meshBuffer!;
        context.CommandList.ExecuteImmediate(list =>
        {
            list.ResourceBarrierTransition(
                meshBuffer,
                ResourceStates.CopyDest,
                ResourceStates.VertexAndConstantBuffer | ResourceStates.IndexBuffer);
        });

    }

    public void Dispose()
    {
        meshBuffer.Dispose();
    }

}