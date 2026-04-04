
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.Direct3D12;

public sealed class FrameResource : IDisposable
{
    public uint FrameIndex;
    
    public ID3D12Resource RenderTarget;
    public CpuDescriptorHandle RenderTargetHandle;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList CommandList;

    //Constant Buffer
    public ID3D12Resource ConstantBuffer;
    public CpuDescriptorHandle ConstantHandle;

    public unsafe Constants* MappedConstants;
    public unsafe ref Constants Constants => ref *MappedConstants;
    

    public ID3D11Resource WrappedBackBuffer;
    public ID2D1Bitmap1 D2DTarget;

    public ulong FenceValue;

    public FrameResource(uint i, ID3D12Device device)
    {
        FrameIndex = i;

        CommandAllocator = device.CreateCommandAllocator(CommandListType.Direct);
        CommandList = device.CreateCommandList<ID3D12GraphicsCommandList>(
            0,
            CommandListType.Direct,
            CommandAllocator,
            null);

        CommandList.Close();
    }

    public void Dispose()
    {
        D2DTarget?.Dispose();
        WrappedBackBuffer?.Dispose();

        ConstantBuffer?.Dispose();
        CommandList?.Dispose();
        CommandAllocator?.Dispose();
        RenderTarget?.Dispose();
    }
}