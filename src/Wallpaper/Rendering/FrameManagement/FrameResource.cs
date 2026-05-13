using Renderer.Resources;
using Settings;
using Vortice.Direct3D12;

namespace Renderer.FrameManagement;

public sealed class FrameResource : IDisposable
{
    internal uint FrameIndex;

    public required ID3D12Resource RenderTarget;
    public required CpuDescriptorHandle RenderTargetHandle;

    public required ID3D12Resource DepthStencil ;
    public required CpuDescriptorHandle DepthStencilHandle ;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList CommandList;

    private ConstantBinding[] ConstantBindings;

    public FrameMetric FrameMetric;
    public SystemSettings Settings;

    public ulong FenceValue;

    public FrameResource(uint i, ID3D12Device device, ConstantBinding[] ConstantBindings)
    {
        FrameIndex = i;
        this.ConstantBindings = ConstantBindings;

        CommandAllocator = device.CreateCommandAllocator(CommandListType.Direct);
        CommandList = device.CreateCommandList<ID3D12GraphicsCommandList>(
            0,
            CommandListType.Direct,
            CommandAllocator,
            null);

        CommandList.Close();
    }

    public ref T GetBufferConstantRef<T>(ConstantBufferKey<T> key) where T : unmanaged
        => ref ConstantBindings[key.Key].Constants<T>();
        
    public ulong GetGPUVirtualAddress(IConstantBufferKey key)
        => ConstantBindings[key.Key].ConstantBuffer.GPUVirtualAddress;

    public void Dispose()
    {
        foreach (var bind in ConstantBindings)
            bind.ConstantBuffer?.Dispose();

        CommandList?.Dispose();
        CommandAllocator?.Dispose();
        RenderTarget?.Dispose();
    }
}