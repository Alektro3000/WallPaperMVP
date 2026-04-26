
using Vortice.Direct3D12;

namespace Renderer.Descriptors;

public readonly struct ResourceDescriptor
{
    public readonly CpuDescriptorHandle Cpu;
    public readonly GpuDescriptorHandle Gpu;

    public ResourceDescriptor(CpuDescriptorHandle cpu, GpuDescriptorHandle gpu)
    {
        Cpu = cpu;
        Gpu = gpu;
    }
    public readonly void Deconstruct(
        out CpuDescriptorHandle cpu,
        out GpuDescriptorHandle gpu)
    {
        cpu = Cpu;
        gpu = Gpu;
    }

}
