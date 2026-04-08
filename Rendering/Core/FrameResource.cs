
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
        => ConstantBindings[key.key] = binding;
    
    public ref T GetBufferConstantRef<T>(ConstantKey key) where T : unmanaged
        => ref ConstantBindings[key.key].Constants<T>();
    public ulong GetGPUVirtualAddress(ConstantKey key)
        => ConstantBindings[key.key].ConstantBuffer.GPUVirtualAddress;

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