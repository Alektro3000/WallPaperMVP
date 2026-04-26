
using Renderer.Descriptors;
using Vortice.Direct3D12;
using Particles.Core;
using Particles.Resources;
using Renderer.Resources;

namespace Particles.Systems.Mouse;

public sealed class Buffer : IDisposable
{
    public readonly ID3D12Resource AliveList;
    public readonly ID3D12Resource BlockSum;
    public readonly ID3D12Resource DispatchArgs;

    public readonly GpuDescriptorHandle UavsStart; // u1..u5
    public readonly GpuDescriptorHandle SrvsStart; // t2..t5

    public Buffer(
        ID3D12Device device,
        HeapAllocator heap,
        ParticleBuffers particleBuffers,
        uint capacity)
    {
        AliveList = CreateActiveListBuffer(device, capacity);
        AliveList.Name = "Mouse AliveList";
        BlockSum = CreateBlockSumBuffer(device, capacity);
        BlockSum.Name = "Mouse BlockList";
        DispatchArgs = CreateDispatchArgsBuffer(device);
        DispatchArgs.Name = "Mouse Compute Dispatch Args";

        UavsStart = CreateMouseUavTable(device, heap, capacity, particleBuffers);
        SrvsStart = CreateMouseSrvTable(device, heap, capacity);
    }
    private GpuDescriptorHandle CreateMouseUavTable(ID3D12Device device, HeapAllocator heap, uint capacity,  ParticleBuffers buffers)
    {
        var range = heap.Allocate(6);
        device.CreateUnorderedAccessView(buffers.EmitterBuffer, null, BufferFactory.CreateStructuredBufferUavDesc<Emitter>(1), range[0].Cpu);
        device.CreateUnorderedAccessView(AliveList, null, BufferFactory.CreateStructuredBufferUavDesc<uint>(capacity), range[1].Cpu);
        device.CreateUnorderedAccessView(BlockSum, null, BufferFactory.CreateStructuredBufferUavDesc<uint>(BlockSumCapacity(capacity)), range[2].Cpu);
        //device.CreateUnorderedAccessView(Counters, null, BufferHelper.CreateStructuredBufferUavDesc<GpuMouseBuffer>(1), range[3].Cpu);
        device.CreateUnorderedAccessView(DispatchArgs, null, BufferFactory.CreateStructuredBufferUavDesc<DispatchArguments>(1), range[4].Cpu);
        device.CreateUnorderedAccessView(buffers.DrawArgs, null, BufferFactory.CreateStructuredBufferUavDesc<DrawIndexedArguments>(1), range[5].Cpu);

        return range[0].Gpu;
    }
    private GpuDescriptorHandle CreateMouseSrvTable(ID3D12Device device, HeapAllocator heap, uint capacity)
    {
        var range = heap.Allocate(3);
        device.CreateShaderResourceView(AliveList, BufferFactory.CreateStructuredBufferSrvDesc<uint>(capacity), range[0].Cpu);
        device.CreateShaderResourceView(BlockSum, BufferFactory.CreateStructuredBufferSrvDesc<uint>(BlockSumCapacity(capacity)), range[1].Cpu);
        //device.CreateShaderResourceView(Counters, BufferHelper.CreateStructuredBufferSrvDesc<GpuMouseBuffer>(1), range[2].Cpu);

        return range[0].Gpu;
    }

    private ID3D12Resource CreateDispatchArgsBuffer(ID3D12Device device)
    {
        return BufferFactory.CreateDefaultBuffer<DispatchArguments>(device, 1,
            ResourceStates.IndirectArgument,
            ResourceFlags.AllowUnorderedAccess);
    }

    private ID3D12Resource CreateActiveListBuffer(ID3D12Device device, uint capacity)
    {
        return BufferFactory.CreateDefaultBuffer<uint>(device, capacity,
            ResourceStates.NonPixelShaderResource,
            ResourceFlags.AllowUnorderedAccess);
    }
    private uint BlockSumCapacity(uint capacity)
    {
        return (capacity+255)/256;
    }
    private ID3D12Resource CreateBlockSumBuffer(ID3D12Device device, uint capacity)
    {
        return BufferFactory.CreateDefaultBuffer<uint>(device, BlockSumCapacity(capacity),
            ResourceStates.NonPixelShaderResource,
            ResourceFlags.AllowUnorderedAccess);
    }

    public void Dispose()
    {
        AliveList?.Release();
        DispatchArgs?.Release();
        BlockSum?.Release();
    }
}