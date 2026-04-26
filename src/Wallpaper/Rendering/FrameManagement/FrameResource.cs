using Renderer.Resources;
using Vortice.Direct3D12;

namespace Renderer.FrameManagement;

public sealed class FrameResource : IDisposable
{
    public uint FrameIndex;
    
    public required ID3D12Resource RenderTarget;
    public required CpuDescriptorHandle RenderTargetHandle;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList CommandList;

    public ConstantBinding[]? ConstantBindings;

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

    public void AddBuffer(FrameManager.ConstantKey key, ConstantBinding binding)
        => ConstantBindings![key.key] = binding;
    
    public ref T GetBufferConstantRef<T>(FrameManager.ConstantKey key) where T : unmanaged
        => ref ConstantBindings![key.key].Constants<T>();
    public ulong GetGPUVirtualAddress(FrameManager.ConstantKey key)
        => ConstantBindings![key.key].ConstantBuffer.GPUVirtualAddress;

    public void Dispose()
    {
        if(ConstantBindings is not null)
            foreach(var bind in ConstantBindings)
                bind.ConstantBuffer?.Dispose();

        CommandList?.Dispose();
        CommandAllocator?.Dispose();
        RenderTarget?.Dispose();
    }
}