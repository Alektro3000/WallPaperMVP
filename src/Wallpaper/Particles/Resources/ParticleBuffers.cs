
using System.ComponentModel;
using System.Data.Common;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Renderer;
using Renderer.Commands;
using Renderer.Core;
using Renderer.Descriptors;
using Renderer.Resources;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Particles.Resources;

public class ParticleBuffers : IDisposable
{
    readonly public uint particleCount;
    public const int particleBuffersLength = 2;

    public struct ParticleBufferBinding
    {
        public ResourceDescriptor ParticleBufferSRV;
        public ResourceDescriptor ParticleBufferUAV;
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

    public ID3D12Resource DrawArgs;


    public ParticleBuffers(ID3D12Device device, ImmediateCommandList commandList, HeapAllocator heapAllocator, string name, Particle[] initParticles)
    {
        particleCount = (uint)initParticles.Length;
        DrawArgs = InitDrawArgs(device, commandList, particleCount, name);
        InitPingPong(device, commandList, heapAllocator, initParticles, name);
    }

    public ParticleBuffers(ID3D12Device device, ImmediateCommandList commandList, HeapAllocator heapAllocator, string name, uint particleCount)
    {
        this.particleCount = particleCount;
        DrawArgs = InitDrawArgs(device, commandList, particleCount, name);
        InitPingPong(device, commandList, heapAllocator, generateParticles(), name);
    }
    private ID3D12Resource InitDrawArgs(ID3D12Device device, ImmediateCommandList commandList, uint particleCount, string name)
    {
        var buf = BufferFactory.CreateDefaultBuffer(device, [new DrawIndexedArguments(GeometryBuffers.IndexCount, particleCount, 0, 0, 0)], commandList,
            ResourceStates.IndirectArgument,
            ResourceFlags.AllowUnorderedAccess);
        buf.Name = name + "_DrawArgs";
        return buf;
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
    private void InitPingPong(ID3D12Device device, ImmediateCommandList commandList, HeapAllocator heapAllocator, Particle[] initParticles, string name)
    {

        for (int i = 0; i < particleBuffersLength; i++)
        {
            var srv = heapAllocator.Allocate()[0];

            var uavRange = heapAllocator.Allocate(1 + 1);

            var buffer = BufferFactory.CreateDefaultBuffer<Particle>(device, initParticles, commandList,
                ResourceStates.VertexAndConstantBuffer,
                ResourceFlags.AllowUnorderedAccess);
            buffer.Name = name + "_ParticleBuffer" + i;

            device.CreateShaderResourceView(buffer, BufferFactory.CreateStructuredBufferSrvDesc<Particle>(particleCount), srv.Cpu);

            device.CreateUnorderedAccessView(buffer, null, BufferFactory.CreateStructuredBufferUavDesc<Particle>(particleCount), uavRange[0].Cpu);

            var bufferBinding = new ParticleBufferBinding()
            {
                ParticleBufferSRV = srv,

                ParticleBufferUAV = uavRange[0],
                ParticleBuffer = buffer,
            };

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
        DrawArgs?.Dispose();
    }
}