using System.Runtime.InteropServices;
using Renderer.Commands;
using Renderer.Descriptors;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Particles.Systems.Fluid;

[StructLayout(LayoutKind.Sequential)]
public struct HashEntry
{
    public uint Hash;
    public uint ParticleIndex;
}

[StructLayout(LayoutKind.Sequential)]
public struct CellRange
{
    public uint Start;
    public uint Count;
}

public sealed class FluidBuffers : IDisposable
{
    public readonly ID3D12Resource HashEntries;
    public readonly ID3D12Resource CellRanges;
    public readonly uint Capacity;

    public FluidBuffers(ID3D12Device device, ImmediateCommandList commandList, uint capacity)
    {
        Capacity = capacity;
        HashEntries = BufferFactory.CreateDefaultBuffer<HashEntry>(
            device,
            capacity,
            ResourceStates.NonPixelShaderResource,
            ResourceFlags.AllowUnorderedAccess);
        HashEntries.Name = "Fluid_HashEntries";

        CellRanges = BufferFactory.CreateDefaultBuffer<CellRange>(
            device,
            FluidCompute.MaxGridCells,
            ResourceStates.NonPixelShaderResource,
            ResourceFlags.AllowUnorderedAccess);
        CellRanges.Name = "Fluid_CellRanges";
    }

    public ResourceDescriptorRange CreateUavTable(ID3D12Device device, HeapAllocator heap)
    {
        var range = heap.Allocate(2);
        device.CreateUnorderedAccessView(HashEntries, null, BufferFactory.CreateStructuredBufferUavDesc<HashEntry>(Capacity), range[0].Cpu);
        device.CreateUnorderedAccessView(CellRanges, null, BufferFactory.CreateStructuredBufferUavDesc<CellRange>(FluidCompute.MaxGridCells), range[1].Cpu);
        return range;
    }

    public ResourceDescriptorRange CreateSrvTable(ID3D12Device device, HeapAllocator heap)
    {
        var range = heap.Allocate(2);
        device.CreateShaderResourceView(HashEntries, BufferFactory.CreateStructuredBufferSrvDesc<HashEntry>(Capacity), range[0].Cpu);
        device.CreateShaderResourceView(CellRanges, BufferFactory.CreateStructuredBufferSrvDesc<CellRange>(FluidCompute.MaxGridCells), range[1].Cpu);
        return range;
    }

    public void Dispose()
    {
        HashEntries.Dispose();
        CellRanges.Dispose();
    }
}
