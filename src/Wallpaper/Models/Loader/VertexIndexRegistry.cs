using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Models;
using Models.Loader;
using Renderer.Commands;
using Renderer.Resources;
using Vortice.Direct3D12;

public class VertexIndexRegistry(InitContext context)
{
    public List<PrimitiveLoader> usedLoaders = [];
    public List<long> vertexOffsets = [];
    public List<int> vertexStrides = [];
    public List<long> indexOffsets = [];
    public List<int> indexSizes = [];
    public long TotalVertexBytes = 0;
    public long TotalIndexOffset = 0;

    public long vertexSize;
    public long indexOffset;
    public long indexSize;


    public ID3D12Resource? meshBuffer;
    ID3D12Device device = context.GraphicsContext.Device;
    ImmediateCommandList immidiateCommandList = context.CommandList;
    
    
    public void AddPrimitive(long vertexCount, long indexCount, int indexSize, int vertexStride, PrimitiveLoader loader)
    {
        usedLoaders.Add(loader);
        vertexOffsets.Add(TotalVertexBytes);
        vertexStrides.Add(vertexStride);
        indexOffsets.Add(TotalIndexOffset);
        indexSizes.Add(indexSize);

        TotalVertexBytes += vertexCount * vertexStride;
        TotalIndexOffset += indexSize * indexCount;
    }

    public void CreateBuffer()
    {
        vertexSize = TotalVertexBytes;

        indexOffset = (vertexSize + 3) / 4 * 4;

        indexSize = TotalIndexOffset;

        meshBuffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)(indexOffset + indexSize), ResourceFlags.None),
            ResourceStates.CopyDest);
    }

    //primitiveIndex is equal to index of primitive added in AddPrimitive function
    public unsafe (VertexBufferView, IndexBufferView) UploadPrimitive(int primitiveIndex, ReadOnlySpan<byte> vertices, IList<uint> indeces)
    {
        if (meshBuffer == null)
        {
            throw new InvalidOperationException(
            "Cannot upload primitive before the mesh buffer has been created. Call CreateBuffer() after registering all primitives.");
        }

        var indexBegin = indexOffsets[primitiveIndex];
        var indexSize = indexSizes[primitiveIndex];
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
                            $"Primitive {primitiveIndex} uses index {index}, which does not fit in 16-bit indices.");

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


        var vertexBufferOffset = vertexOffsets[primitiveIndex];

        var vertexBufferSize = vertices.Length * sizeof(byte);
        using var vertexUploadBuffer = BufferFactory.CreateUploadBuffer(device, vertices);

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
        var vertexBufferView = new VertexBufferView
        {
            BufferLocation = meshBuffer.GPUVirtualAddress + (ulong)vertexBufferOffset,
            SizeInBytes = (uint)vertexBufferSize,
            StrideInBytes = (uint)vertexStrides[primitiveIndex]
        };
        return (vertexBufferView, indexBufferView);
    }

}
