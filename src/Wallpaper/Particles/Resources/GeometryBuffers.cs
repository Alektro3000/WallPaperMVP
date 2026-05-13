
using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using Renderer.Core;
using Renderer.Descriptors;
using Renderer;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Renderer.Commands;
using Renderer.Resources;

namespace Particles.Resources;
public class GeometryBuffers : IDisposable
{
    public const uint IndexCount = 6;

    public ID3D12Resource IndexBuffer  { get; }
    public ID3D12Resource VertexBuffer  { get; }

    public IndexBufferView IndexBufferView {get; }
    public VertexBufferView VertexBufferView {get; }
    
    
    public GeometryBuffers(ID3D12Device device, ImmediateCommandList commandList, HeapAllocator heapAllocator)
    {        
        QuadVertex[] quadVertices =
        [
            new QuadVertex { LocalOffset = new Vector2(-0.5f, -0.5f), UV = new Vector2(0, 1) },
            new QuadVertex { LocalOffset = new Vector2( 0.5f, -0.5f), UV = new Vector2(1, 1) },
            new QuadVertex { LocalOffset = new Vector2( 0.5f,  0.5f), UV = new Vector2(1, 0) },
            new QuadVertex { LocalOffset = new Vector2(-0.5f,  0.5f), UV = new Vector2(0, 0) },
        ];

        VertexBuffer = BufferFactory.CreateDefaultBuffer<QuadVertex>(device, quadVertices, commandList);
        VertexBuffer.Name = "VertexBuffer";
        VertexBufferView = BufferFactory.CreateVertexBufferView<QuadVertex>(VertexBuffer, (uint)quadVertices.Length);
        


        ushort[] quadIndices = [0, 1, 2, 0, 2, 3];
        IndexBuffer = BufferFactory.CreateDefaultBuffer<ushort>(device, quadIndices, commandList);
        IndexBuffer.Name = "IndexBuffer";
        IndexBufferView = BufferFactory.CreateIndexBufferView(IndexBuffer, (uint)quadIndices.Length);


    }

    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
    }
}