
using System.Reflection.Metadata;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.Direct3D12;
using static FrameManager;

public sealed class FrameResource : IDisposable
{
    public uint FrameIndex;
    
    public ID3D12Resource RenderTarget;
    public CpuDescriptorHandle RenderTargetHandle;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList CommandList;

    //Constant Buffer
    public struct ConstantBinding
    {
        public ID3D12Resource ConstantBuffer;

        public unsafe byte* MappedConstants;
        public unsafe ref T Constants<T>() where T : unmanaged => ref *(T*)MappedConstants;
    }
    public ConstantBinding[] ConstantBindings;

    public ID3D11Resource WrappedBackBuffer;
    public ID2D1Bitmap1 D2DTarget;
    public FrameMetric frameMetric;

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

    public void AddBuffer(ConstantKey key, ConstantBinding binding)
    {
        ConstantBindings[key.key] = binding;
    }
    public ConstantBinding GetBuffer(ConstantKey key)
    {
        return ConstantBindings[key.key];
    }

    public void Dispose()
    {
        D2DTarget?.Dispose();
        WrappedBackBuffer?.Dispose();

        foreach(var bind in ConstantBindings)
            bind.ConstantBuffer?.Dispose();

        CommandList?.Dispose();
        CommandAllocator?.Dispose();
        RenderTarget?.Dispose();
    }
}