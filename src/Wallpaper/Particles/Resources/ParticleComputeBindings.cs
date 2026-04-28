
using Renderer.Descriptors;
using Vortice.Direct3D12;
using Particles.Core;
using Particles.Resources;
using Renderer.Resources;
using Renderer.Commands;
using Renderer.FrameManagement;

namespace Particles.Resources;

public sealed class ParticleComputeBindings : IDisposable
{

    public readonly GpuDescriptorHandle UavsStart; // u1..u5
    public readonly GpuDescriptorHandle SrvsStart; // t2..t5
    
    public readonly Shared.Global.Buffers CommonBuffers;
    public readonly Shared.Field.Buffers FieldBuffers;
    public readonly ParticleBuffers ParticleBuffers;
    public readonly ComputeBuffers ComputeBuffers;

    public ParticleComputeBindings(
        ID3D12Device device,
        HeapAllocator heap,
        ParticleBuffers particleBuffers,
        Shared.Global.Buffers commonBuffers,
        Shared.Field.Buffers fieldBuffers,
        ImmediateCommandList commandList,
        string name,
        uint capacity)
    {
        ParticleBuffers = particleBuffers; 
        CommonBuffers = commonBuffers;
        FieldBuffers = fieldBuffers;
        ComputeBuffers = new ComputeBuffers(device, commandList, name, capacity);
        UavsStart = CreateUavTable(device, heap, capacity);
        SrvsStart = CreateSrvTable(device, heap, capacity);
    }
    private GpuDescriptorHandle CreateUavTable(ID3D12Device device, HeapAllocator heap, uint capacity)
    {
        var range = heap.Allocate(5);
        device.CreateUnorderedAccessView(ComputeBuffers.EmitterBuffer, null, BufferFactory.CreateStructuredBufferUavDesc<Emitter>(1), range[0].Cpu);
        device.CreateUnorderedAccessView(ComputeBuffers.AliveList, null, BufferFactory.CreateStructuredBufferUavDesc<uint>(capacity), range[1].Cpu);
        device.CreateUnorderedAccessView(ComputeBuffers.BlockSum, null, BufferFactory.CreateStructuredBufferUavDesc<uint>(ComputeBuffers.BlockSumCapacity(capacity)), range[2].Cpu);
        device.CreateUnorderedAccessView(ComputeBuffers.DispatchArgs, null, BufferFactory.CreateStructuredBufferUavDesc<DispatchArguments>(1), range[3].Cpu);
        device.CreateUnorderedAccessView(ParticleBuffers.DrawArgs, null, BufferFactory.CreateStructuredBufferUavDesc<DrawIndexedArguments>(1), range[4].Cpu);

        return range[0].Gpu;
    }
    private GpuDescriptorHandle CreateSrvTable(ID3D12Device device, HeapAllocator heap, uint capacity)
    {
        var range = heap.Allocate(2);
        device.CreateShaderResourceView(ComputeBuffers.AliveList, BufferFactory.CreateStructuredBufferSrvDesc<uint>(capacity), range[0].Cpu);
        device.CreateShaderResourceView(ComputeBuffers.BlockSum, BufferFactory.CreateStructuredBufferSrvDesc<uint>(ComputeBuffers.BlockSumCapacity(capacity)), range[1].Cpu);

        return range[0].Gpu;
    }
    public void Dispose()
    {
        ComputeBuffers.Dispose();
    }
}