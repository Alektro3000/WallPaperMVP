using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Models;
using Renderer.Commands;
using Renderer.Resources;
using Vortice.Direct3D12;

public class VertexIndexRegistry
{
    public List<long> vertexOffsets = [];
    public List<long> indexOffsets = [];
    public List<int> indexSizes = [];
    public long TotalVertexCount = 0;
    public long TotalIndexCount = 0;
    public long TotalIndexOffset = 0;

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
    public void AddPrimitive(long VertexCount, long IndexCount, int indexSize)
    {
        vertexOffsets.Add(TotalVertexCount);
        indexOffsets.Add(TotalIndexOffset);
        indexSizes.Add(indexSize);

        TotalVertexCount += VertexCount;
        TotalIndexCount += IndexCount;
        TotalIndexOffset += indexSize * IndexCount;
    }

    public void CreateBuffer()
    {
        vertexSize = TotalVertexCount * Marshal.SizeOf<StaticVertex>();

        indexOffset = (vertexSize + 3) / 4 * 4;

        indexSize = TotalIndexOffset;

        meshBuffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)(indexOffset + indexSize), ResourceFlags.None),
            ResourceStates.CopyDest);
    }

    public unsafe (VertexBufferView, IndexBufferView) UploadPrimitive(int v, StaticVertex[] vertices, IList<uint> indeces)
    {
        if (meshBuffer == null)
        {
            throw new InvalidOperationException(
            "Cannot upload primitive before the mesh buffer has been created. Call CreateBuffer() after registering all primitives.");
        }

        var indexBegin = indexOffsets[v];
        var indexSize = indexSizes[v];
        var indexBufferOffset = indexOffset + indexBegin;

        var indexCount = indeces.Count;
        var indexBufferSize = indexCount * indexSize;

        using ID3D12Resource IndexUploadBuffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)indexBufferSize),
            ResourceStates.GenericRead);

        //Copy data to buffer
        void* mapped = null;
        IndexUploadBuffer.Map(0, null, &mapped).CheckError();

        try
        {
            if (indexSize == 2)
            {
                ushort* dst16 = (ushort*)mapped;

                for (int i = 0; i < indexCount; i++)
                {
                    uint index = indeces[i];

                    if (index > ushort.MaxValue)
                        throw new InvalidOperationException(
                            $"Primitive {v} uses index {index}, which does not fit in 16-bit indices.");

                    dst16[i] = (ushort)index;
                }
            }
            else if (indexSize == 4)
            {
                uint* dst32 = (uint*)mapped;

                for (int i = 0; i < indexCount; i++)
                    dst32[i] = indeces[i];
            }
            else
            {
                throw new Exception($"Unsupported D3D12 index size: {indexSize}");
            }

        }
        finally
        {
            IndexUploadBuffer.Unmap(0);
        }


        var vertexBegin = vertexOffsets[v];
        var vertexBufferOffset = vertexBegin * Marshal.SizeOf<StaticVertex>();

        var vertexBufferSize = vertices.Length * Marshal.SizeOf<StaticVertex>();
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

        var indexBufferView = BufferFactory.CreateIndexBufferView(meshBuffer, (uint)indexCount, (uint)indexBufferOffset, indexSize);
        var vertexBufferView = BufferFactory.CreateVertexBufferView<StaticVertex>(meshBuffer, (uint)vertices.Length, (uint)vertexBufferOffset);
        return (vertexBufferView, indexBufferView);
    }

}
