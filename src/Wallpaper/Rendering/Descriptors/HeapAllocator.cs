

using Vortice.Direct3D12;

namespace Renderer.Descriptors;

public class HeapAllocator : IDisposable
{
    private const uint AllocatorInitSize = 256;
    private readonly ID3D12DescriptorHeap heap;
    private uint CurrentOffset;
    private readonly uint descriptorSize;

    public HeapAllocator(ID3D12Device device)
    {
        var heapDesc = new DescriptorHeapDescription(
            DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            AllocatorInitSize,
            DescriptorHeapFlags.ShaderVisible,
            0);
        descriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        heap = device.CreateDescriptorHeap(heapDesc);

    }

    public ResourceDescriptorRange Allocate(uint size = 1)
    {
        if (CurrentOffset + size > AllocatorInitSize)
            throw new OutOfMemoryException("Out of memory for descriptor heap.");

        var _baseCpu = heap.GetCPUDescriptorHandleForHeapStart();
        var _baseGpu = heap.GetGPUDescriptorHandleForHeapStart();

        _baseCpu = new CpuDescriptorHandle(in _baseCpu, (int)CurrentOffset, descriptorSize);
        _baseGpu = new GpuDescriptorHandle(in _baseGpu, (int)CurrentOffset, descriptorSize);

        CurrentOffset += size;
        return new ResourceDescriptorRange(_baseCpu, _baseGpu, size, descriptorSize);
    }

    public void BindForCommandList(ID3D12GraphicsCommandList cmd)
    {
        cmd.SetDescriptorHeaps(heap);
    }

    public void Dispose()
    {
        heap?.Dispose();
    }
}
