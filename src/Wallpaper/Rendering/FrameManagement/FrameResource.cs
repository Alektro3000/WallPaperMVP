using Renderer.Resources;
using Vortice.Direct3D12;

namespace Renderer.FrameManagement;

public sealed class FrameResource : IDisposable
{
    internal uint FrameIndex;

    public required ID3D12Resource RenderTarget;
    public required CpuDescriptorHandle RenderTargetHandle;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList CommandList;

    public required ConstantBinding[] ConstantBindings;

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

    public void AddBuffer(ConstantBufferKey key, ConstantBinding binding)
        => ConstantBindings[key.Key] = binding;

    public ref T GetBufferConstantRef<T>(ConstantBufferKey key) where T : unmanaged
        => ref ConstantBindings[key.Key].Constants<T>();
    public ulong GetGPUVirtualAddress(ConstantBufferKey key)
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