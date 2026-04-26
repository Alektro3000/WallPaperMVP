
using Vortice.Direct3D12;

namespace Renderer.Descriptors;

public struct ResourceDescriptorRange
{
    private readonly CpuDescriptorHandle Cpu;
    private readonly GpuDescriptorHandle Gpu;
    public readonly uint Size;

    public ResourceDescriptorRange(CpuDescriptorHandle cpu, GpuDescriptorHandle gpu, uint size)
    {
        Cpu = cpu;
        Gpu = gpu;
        Size = size;
    }

    public readonly ResourceDescriptor this[int id]
    {
        get
        {
            if (id >= Size)
                throw new IndexOutOfRangeException("Out of range exception for Resource Descriptor");
            return new(
                new CpuDescriptorHandle(Cpu, id, HeapAllocator.DescriptorSize),
                new GpuDescriptorHandle(Gpu, id, HeapAllocator.DescriptorSize));
        }
    }
}
