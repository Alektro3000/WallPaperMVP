

using Vortice.Direct3D12;

namespace Renderer.Descriptors;

public class HeapAllocator : IDisposable
{
    private const uint AllocatorInitSize = 64;
    public readonly ID3D12DescriptorHeap Heap;
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
        Heap = device.CreateDescriptorHeap(heapDesc);

    }

    public ResourceDescriptorRange Allocate(uint size = 1)
    {
        if (CurrentOffset + size > AllocatorInitSize)
            throw new OutOfMemoryException("Out of memory for descriptor heap.");

        var _baseCpu = Heap.GetCPUDescriptorHandleForHeapStart();
        var _baseGpu = Heap.GetGPUDescriptorHandleForHeapStart();

        _baseCpu = new CpuDescriptorHandle(in _baseCpu, (int)CurrentOffset, DescriptorSize);
        _baseGpu = new GpuDescriptorHandle(in _baseGpu, (int)CurrentOffset, DescriptorSize);

        CurrentOffset += size;
        return new ResourceDescriptorRange(_baseCpu, _baseGpu, size);
    }

    public void Dispose()
    {
        Heap?.Dispose();
    }
}