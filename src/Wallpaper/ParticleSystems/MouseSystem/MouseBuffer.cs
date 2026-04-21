using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;

public sealed class MouseBuffer : IDisposable
{
    public readonly ID3D12Resource AliveList;
    public readonly ID3D12Resource Counters;
    public readonly ID3D12Resource DispatchArgs;

    public readonly GpuDescriptorHandle UavsStart; // u1..u4
    public readonly GpuDescriptorHandle SrvsStart; // t2..t4

    public MouseBuffer(
        ID3D12Device device,
        HeapAllocator heap,
        ParticleBuffers particleBuffers,
        uint capacity)
    {
        AliveList = CreateActiveListBuffer(device, capacity);
        Counters = CreateCountersBuffer(device);
        DispatchArgs = CreateDispatchArgsBuffer(device);

        UavsStart = CreateMouseUavTable(device, heap, capacity, particleBuffers, AliveList, Counters, DispatchArgs);
        SrvsStart = CreateMouseSrvTable(device, heap, capacity, AliveList, Counters);
    }
    private GpuDescriptorHandle CreateMouseUavTable(ID3D12Device device, HeapAllocator heap, uint capacity,  ParticleBuffers buffers, ID3D12Resource activeList, ID3D12Resource counters, ID3D12Resource dispatchArgs)
    {
        var range = heap.Allocate(5);
        device.CreateUnorderedAccessView(buffers.EmitterBuffer, null, BufferHelper.CreateStructuredBufferUavDesc<Emitter>(1), range[0].Cpu);
        device.CreateUnorderedAccessView(activeList, null, BufferHelper.CreateStructuredBufferUavDesc<uint>(capacity), range[1].Cpu);
        device.CreateUnorderedAccessView(counters, null, BufferHelper.CreateStructuredBufferUavDesc<GpuMouseBuffer>(1), range[2].Cpu);
        device.CreateUnorderedAccessView(dispatchArgs, null, BufferHelper.CreateStructuredBufferUavDesc<DispatchArgs>(1), range[3].Cpu);
        device.CreateUnorderedAccessView(buffers.DrawArgs, null, BufferHelper.CreateStructuredBufferUavDesc<DrawIndexedArguments>(1), range[4].Cpu);

        return range[0].Gpu;
    }
    private GpuDescriptorHandle CreateMouseSrvTable(ID3D12Device device, HeapAllocator heap, uint capacity, ID3D12Resource activeList, ID3D12Resource counters)
    {
        var range = heap.Allocate(2);
        device.CreateShaderResourceView(activeList, BufferHelper.CreateStructuredBufferSrvDesc<uint>(capacity), range[0].Cpu);
        device.CreateShaderResourceView(counters, BufferHelper.CreateStructuredBufferSrvDesc<GpuMouseBuffer>(1), range[1].Cpu);

        return range[0].Gpu;
    }

    private ID3D12Resource CreateDispatchArgsBuffer(ID3D12Device device)
    {
        return BufferHelper.CreateDefaultBuffer<DispatchArgs>(device, 1,
            ResourceStates.IndirectArgument,
            ResourceFlags.AllowUnorderedAccess);
    }

    private ID3D12Resource CreateCountersBuffer(ID3D12Device device)
    {
        return BufferHelper.CreateDefaultBuffer<GpuMouseBuffer>(device, 1,
            ResourceStates.UnorderedAccess,
            ResourceFlags.AllowUnorderedAccess);
    }

    private ID3D12Resource CreateActiveListBuffer(ID3D12Device device, uint capacity)
    {
        return BufferHelper.CreateDefaultBuffer<uint>(device, capacity,
            ResourceStates.UnorderedAccess,
            ResourceFlags.AllowUnorderedAccess);
    }

    public void Dispose()
    {
        AliveList?.Release();
        Counters?.Release();
        DispatchArgs?.Release();
    }
}