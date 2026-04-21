

using Vortice.Direct3D12;

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

public class HeapAllocator : IDisposable
{
    private const uint AllocatorInitSize = 64;
    private ID3D12DescriptorHeap _heap;
    public ID3D12DescriptorHeap Heap { get => _heap; }
    private uint CurrentOffset;
    public static uint DescriptorSize;

    public HeapAllocator(ID3D12Device device)
    {
        var heapDesc = new DescriptorHeapDescription(
            DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            AllocatorInitSize,
            DescriptorHeapFlags.ShaderVisible,
            0);
        DescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        _heap = device.CreateDescriptorHeap(heapDesc);

    }

    public ResourceDescriptorRange Allocate(uint size = 1)
    {
        if (CurrentOffset + size > AllocatorInitSize)
            throw new OutOfMemoryException("Out of memory for descriptor heap.");

        var _baseCpu = _heap.GetCPUDescriptorHandleForHeapStart();
        var _baseGpu = _heap.GetGPUDescriptorHandleForHeapStart();

        _baseCpu = new CpuDescriptorHandle(in _baseCpu, (int)CurrentOffset, DescriptorSize);
        _baseGpu = new GpuDescriptorHandle(in _baseGpu, (int)CurrentOffset, DescriptorSize);

        CurrentOffset += size;
        return new ResourceDescriptorRange(_baseCpu, _baseGpu, size);
    }

    public void Dispose()
    {
        _heap?.Dispose();
    }
}