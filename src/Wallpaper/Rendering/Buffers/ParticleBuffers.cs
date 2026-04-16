
using System.ComponentModel;
using System.Data.Common;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;

public class ParticleBuffers : IDisposable
{
    readonly public uint particleCount;
    public const int particleBuffersLength = 2;

    public struct ParticleBufferBinding
    {
        public CpuDescriptorHandle ParticleBufferSRVCpu;
        public GpuDescriptorHandle ParticleBufferSRVGpu;
        public CpuDescriptorHandle ParticleBufferUAVCpu;
        public GpuDescriptorHandle ParticleBufferUAVGPU;
        public ID3D12Resource ParticleBuffer;
    };

    private ParticleBufferBinding[] Buffers = new ParticleBufferBinding[particleBuffersLength];

    private int _readIndex = 0;
    private int _writeIndex = 1;
    public int ReadIndex { get => _readIndex; }
    public int WriteIndex { get => _writeIndex; }

    public ID3D12Resource ReadBuffer => Buffers[ReadIndex].ParticleBuffer;
    public ID3D12Resource WriteBuffer => Buffers[WriteIndex].ParticleBuffer;
    public ParticleBufferBinding ReadBufferBinding => Buffers[ReadIndex];
    public ParticleBufferBinding WriteBufferBinding => Buffers[WriteIndex];


    public List<ID3D12Resource> ComputeBuffers;


    public ParticleBuffers(ID3D12Device device, ImmediateCommandList commandList, HeapAllocator heapAllocator, Particle[] initParticles)
    {
        particleCount = (uint)initParticles.Length;
        ComputeBuffers = [];
        InitEmitterBuffer(device, commandList);
        InitPingPong(device, commandList, heapAllocator, initParticles);
    }

    public ParticleBuffers(ID3D12Device device, ImmediateCommandList commandList, HeapAllocator heapAllocator, uint particleCount)
    {
        this.particleCount = particleCount;
        ComputeBuffers = [];
        InitEmitterBuffer(device, commandList);
        InitPingPong(device, commandList, heapAllocator, generateParticles());
    }
    private void InitEmitterBuffer(ID3D12Device device, ImmediateCommandList commandList)
    {
        ComputeBuffers.Add(BufferHelper.CreateDefaultBuffer(device, [new Emitter()], commandList,
            ResourceStates.VertexAndConstantBuffer,
            ResourceFlags.AllowUnorderedAccess));
    }
    private Particle[] generateParticles()
    {
        return Enumerable.Range(0, (int)particleCount)
                          .Select(i => new Particle
                          {
                              Position = new Vector2(0.0f, 0.0f),
                              Age = -1f,
                          })
                          .ToArray();

    }
    private void InitPingPong(ID3D12Device device, ImmediateCommandList commandList, HeapAllocator heapAllocator, Particle[] initParticles)
    {
        //
        // 1. Descriptor heap for SRV/UAV
        //

        uint stride = (uint)Marshal.SizeOf<Particle>();


        var srvDesc = new ShaderResourceViewDescription
        {
            ViewDimension = ShaderResourceViewDimension.Buffer,
            Shader4ComponentMapping = ShaderConstants.Shader4ComponentMapping,
            Format = Format.Unknown,
            Buffer = new BufferShaderResourceView
            {
                FirstElement = 0,
                NumElements = particleCount,
                StructureByteStride = stride,
                Flags = BufferShaderResourceViewFlags.None
            }
        };

        var uavDesc = new UnorderedAccessViewDescription
        {
            ViewDimension = UnorderedAccessViewDimension.Buffer,
            Format = Format.Unknown,
            Buffer = new BufferUnorderedAccessView
            {
                FirstElement = 0,
                NumElements = particleCount,
                StructureByteStride = stride,
                CounterOffsetInBytes = 0,
                Flags = BufferUnorderedAccessViewFlags.None
            }
        };

        for (int i = 0; i < particleBuffersLength; i++)
        {
            (var _srvCpu, var _srvGpu) = heapAllocator.Allocate();

            (var _uavCpu, var _uavGpu) = heapAllocator.Allocate(1 + (uint)ComputeBuffers.Count);

            var buffer = BufferHelper.CreateDefaultBuffer<Particle>(device, initParticles, commandList,
                ResourceStates.VertexAndConstantBuffer,
                ResourceFlags.AllowUnorderedAccess);

            device.CreateShaderResourceView(buffer, srvDesc, _srvCpu);

            device.CreateUnorderedAccessView(buffer, null, uavDesc, _uavCpu);

            var bufferBinding = new ParticleBufferBinding()
            {
                ParticleBufferSRVCpu = _srvCpu,
                ParticleBufferSRVGpu = _srvGpu,

                ParticleBufferUAVGPU = _uavGpu,
                ParticleBufferUAVCpu = _uavCpu,
                ParticleBuffer = buffer,
            };
            var EmitterBufferUAVCpu = new CpuDescriptorHandle(_uavCpu, 1, heapAllocator.DescriptorSize);
            bindUAVResource(device, ComputeBuffers[0], EmitterBufferUAVCpu);

            Buffers[i] = bufferBinding;
        }
    }
    public void bindUAVResource(ID3D12Device device, ID3D12Resource resource, CpuDescriptorHandle handle)
    {
        uint strideEmitter = (uint)Marshal.SizeOf<Emitter>();

        var EmitterUAVDesc = new UnorderedAccessViewDescription
        {
            ViewDimension = UnorderedAccessViewDimension.Buffer,
            Format = Format.Unknown,
            Buffer = new BufferUnorderedAccessView
            {
                FirstElement = 0,
                NumElements = 1,
                StructureByteStride = strideEmitter,
                CounterOffsetInBytes = 0,
                Flags = BufferUnorderedAccessViewFlags.None
            }
        };

        device.CreateUnorderedAccessView(resource, null, EmitterUAVDesc, handle);
    }
    public ID3D12Resource this[int id]
    {
        get => Buffers[id].ParticleBuffer;
    }

    public void SwapBuffers()
    {
        (_readIndex, _writeIndex) = (_writeIndex, _readIndex);
    }

    public void Dispose()
    {
        for (int i = 0; i < Buffers.Length; i++)
            Buffers[i].ParticleBuffer.Dispose();
        ComputeBuffers.ForEach(x=>x.Dispose());
    }
}