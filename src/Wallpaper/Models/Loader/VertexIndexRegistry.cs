using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Models;
using Renderer.Commands;
using Renderer.Resources;
using SharpGLTF.Schema2;
using Vortice.Direct3D12;

public class VertexIndexRegistry
{
    public List<long> vertexOffsets = [];
    public List<long> indexOffsets = [];
    public long TotalVertexCount = 0;
    public long TotalIndexCount = 0;
    
    public long vertexSize;
    public long indexOffset;
    public long indexSize;


    public ID3D12Resource? meshBuffer;
    ID3D12Device device;
    ImmediateCommandList immidiateCommandList;
    public VertexIndexRegistry(InitContext context)
    {
        immidiateCommandList = context.CommandList;
        device = context.GraphicsContext.Device;
    }
    public void AddPrimitive(long VertexCount, long IndexCount)
    {
        vertexOffsets.Add(TotalVertexCount);
        indexOffsets.Add(TotalIndexCount);

        TotalVertexCount += VertexCount;
        TotalIndexCount += IndexCount;
    }

    public void CreateBuffer()
    {
        vertexSize = TotalVertexCount * Marshal.SizeOf<StaticVertex>();

        indexOffset = (vertexSize+3)/4 * 4;

        indexSize = TotalIndexCount * Marshal.SizeOf<ushort>();

        meshBuffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)(indexOffset + indexSize), ResourceFlags.None),
            ResourceStates.CopyDest);
    }

    public (VertexBufferView, IndexBufferView) UploadPrimitive(int v, StaticVertex[] vertices, ushort[] indeces)
    {
        if(meshBuffer == null)
        {
            throw new InvalidOperationException(
            "Cannot upload primitive before the mesh buffer has been created. Call CreateBuffer() after registering all primitives.");
        }

        var indexBegin = indexOffsets[v];
        var indexBufferOffset = indexOffset + indexBegin *  Marshal.SizeOf<ushort>();

        var indexBufferSize = indeces.Length  *  Marshal.SizeOf<ushort>();
        using var IndexUploadBuffer = BufferFactory.CreateUploadBuffer<ushort>(device, indeces);
        
        var vertexBegin = vertexOffsets[v];
        var vertexBufferOffset = vertexBegin * Marshal.SizeOf<StaticVertex>();

        var vertexBufferSize = vertices.Length  *  Marshal.SizeOf<StaticVertex>();
        using var vertexUploadBuffer = BufferFactory.CreateUploadBuffer<StaticVertex>(device, vertices);

        immidiateCommandList.ExecuteImmediate(list =>
        {
            list.CopyBufferRegion(
                meshBuffer, (ulong)indexBufferOffset,
                IndexUploadBuffer, 0,
                (ulong)indexBufferSize);
                
            list.CopyBufferRegion(
                meshBuffer, (ulong)vertexBufferOffset,
                vertexUploadBuffer, 0,
                (ulong)vertexBufferSize);
        });
        
        var indexBufferView = BufferFactory.CreateIndexBufferView(meshBuffer, (uint)indeces.Length, (uint)indexBufferOffset);
        var vertexBufferView = BufferFactory.CreateVertexBufferView<StaticVertex>(meshBuffer, (uint)vertices.Length, (uint)vertexBufferOffset);
        return (vertexBufferView, indexBufferView);
    }
    
}
