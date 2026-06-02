using Renderer.Descriptors;
using Renderer.Resources;
using Settings;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Renderer.FrameManagement;

public sealed class FrameResource : IDisposable
{
    private readonly SwapChainHandler screenRenderTarget;
    public required ID3D12Resource RenderTarget;
    public required ResourceDescriptor RenderTargetHandle;
    public required ID3D12Resource DepthStencil;
    public required ResourceDescriptor DepthStencilHandle ;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList CommandList;

    private ConstantBinding[] ConstantBindings;

    public FrameMetric FrameMetric;
    public SystemSettings Settings;

    internal ulong FenceValue;

    public FrameResource(ID3D12Device device, ConstantBinding[] ConstantBindings, SwapChainHandler screenRenderTarget)
    {
        this.ConstantBindings = ConstantBindings;
        this.screenRenderTarget = screenRenderTarget;

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

    public void BindRenderTarget()
    {
        screenRenderTarget.BindForCommandList(CommandList);

        CommandList.OMSetRenderTargets(
            RenderTargetHandle.Cpu, 
            DepthStencilHandle.Cpu);
         

    }
    public void ClearRenderTarget()
    {
        CommandList.ClearRenderTargetView(
            RenderTargetHandle.Cpu, 
            new Color4(0.0f, 0.0f, 0.0f, 0.0f));
            
        CommandList.ClearDepthStencilView(
            DepthStencilHandle.Cpu,
            ClearFlags.Depth,
            1.0f,
            0);
    }
}