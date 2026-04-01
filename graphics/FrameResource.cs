
using Vortice.Direct3D12;

public struct FrameResource : IDisposable
{
    public ID3D12Resource RenderTarget;
    public ID3D12CommandAllocator CommandAllocator;

    //Constant Buffer
    public ID3D12Resource ConstantBuffer;
    public unsafe Constants* MappedConstants;
    public ID3D12DescriptorHeap ConstantBufferHeap;

    public ulong FenceValue;

    public void Dispose()
    {
        ConstantBufferHeap?.Dispose();
        CommandAllocator?.Dispose();
        ConstantBuffer?.Dispose();
        RenderTarget?.Dispose();
    }
}