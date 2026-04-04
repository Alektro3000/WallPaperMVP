

using Vortice.Direct3D12;

public class HeapAllocator
{
    public struct HeapRangeDescriptor
    {
        public CpuDescriptorHandle CpuHandle;
        public GpuDescriptorHandle GpuHandle;
        public readonly void Deconstruct(
            out CpuDescriptorHandle cpuHandle,
            out GpuDescriptorHandle gpuHandle)
        {
            cpuHandle = CpuHandle;
            gpuHandle = GpuHandle;
        }
    }
    private const uint AllocatorInitSize = 64;
    private ID3D12DescriptorHeap _heap;
    public ID3D12DescriptorHeap Heap {get => _heap;}
    private uint CurrentOffset;
    public uint DescriptorSize;
    
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

    public HeapRangeDescriptor Allocate(uint size = 1)
    {
        if(CurrentOffset + size > AllocatorInitSize)
            throw new OutOfMemoryException("Out of memory for descriptor heap.");

        var _baseCpu = _heap.GetCPUDescriptorHandleForHeapStart();
        var _baseGpu = _heap.GetGPUDescriptorHandleForHeapStart();

        _baseCpu = new CpuDescriptorHandle(in _baseCpu, (int)CurrentOffset,DescriptorSize);
        _baseGpu = new GpuDescriptorHandle(in _baseGpu, (int)CurrentOffset,DescriptorSize);

        CurrentOffset += size;
        return new HeapRangeDescriptor()
        {
            CpuHandle = _baseCpu,
            GpuHandle = _baseGpu,
        };
    }
}