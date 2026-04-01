
using System.Numerics;
using Vortice.Direct3D12;
using Vortice.DXGI;

public class ParticleBuffers : IDisposable
{
    private ID3D12Device _device;
    readonly public uint _particleCount = 3;
    public const int particleBuffersLength = 2;

    public ID3D12Resource IndexBuffer  { get; }
    public ID3D12Resource VertexBuffer  { get; }

    public IndexBufferView IndexBufferView {get; }
    public VertexBufferView VertexBufferView {get; }
    
    public ID3D12Resource BufferA  { get; }
    public ID3D12Resource BufferB  { get; }
    private int _readIndex = 0;
    private int _writeIndex = 1;
    public int ReadIndex { get => _readIndex; }
    public int WriteIndex  { get => _writeIndex; }
    public ID3D12Resource ReadBuffer => ReadIndex == 0 ? BufferA : BufferB;
    public ID3D12Resource WriteBuffer => WriteIndex == 0 ? BufferA : BufferB;
    public ParticleBuffers(ID3D12Device device, ImmidiateCommandList commandList)
    {
        _device = device;
        
        Particle[] vertices =
        [
            new Particle { Position = new Vector3( 0.0f,  0.5f, 0.0f), Color = new Vector3(0f,1f,1f) },
            new Particle { Position = new Vector3( 0.5f, -0.5f, 0.0f), Color = new Vector3(1f,0f,1f) },
            new Particle { Position = new Vector3(-0.5f, -0.5f, 0.0f), Color = new Vector3(1f,1f,0f) },
        ];
        BufferA = BufferHelper.CreateDefaultBuffer(_device, vertices, commandList, ResourceStates.VertexAndConstantBuffer, ResourceFlags.AllowUnorderedAccess);
        BufferB = BufferHelper.CreateDefaultBuffer(_device, vertices, commandList, ResourceStates.VertexAndConstantBuffer, ResourceFlags.AllowUnorderedAccess);

        
        QuadVertex[] quadVertices =
        [
            new QuadVertex { LocalOffset = new Vector2(-0.5f, -0.5f), UV = new Vector2(0, 1) },
            new QuadVertex { LocalOffset = new Vector2( 0.5f, -0.5f), UV = new Vector2(1, 1) },
            new QuadVertex { LocalOffset = new Vector2( 0.5f,  0.5f), UV = new Vector2(1, 0) },
            new QuadVertex { LocalOffset = new Vector2(-0.5f,  0.5f), UV = new Vector2(0, 0) },
        ];

        VertexBuffer = BufferHelper.CreateDefaultBuffer(_device, quadVertices, commandList);
        VertexBufferView = BufferHelper.CreateVertexBufferView<QuadVertex>(VertexBuffer, (uint)quadVertices.Length);
        


        ushort[] quadIndices = [0, 1, 2, 0, 2, 3];
        IndexBuffer = BufferHelper.CreateDefaultBuffer(_device, quadIndices, commandList);
        IndexBufferView = BufferHelper.CreateIndexBufferView(IndexBuffer, (uint)quadIndices.Length);

    }
    public ID3D12Resource this[int id]
    {
        get => id == 0 ? BufferA : BufferB;
    }

    public void SwapBuffers()
    {
        (_readIndex , _writeIndex) = (_writeIndex, _readIndex );
    }

    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
        BufferA.Dispose();
        BufferB.Dispose();
    }
}