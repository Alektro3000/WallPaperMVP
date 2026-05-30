
using Renderer.Core;
using Renderer.Descriptors;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Renderer.FrameManagement;


public sealed class SwapChainHandler : IDisposable
{
    private readonly IDXGISwapChain3 swapChain;
    private readonly ID3D12DescriptorHeap rtvHeap;
    private readonly Viewport viewport;
    private readonly RectI scissor;

    public uint CurrentBackBufferIndex => swapChain.CurrentBackBufferIndex;

    public SwapChainHandler(GraphicsContext context, IntPtr hwnd, int width, int height, uint frameCount)
    {
        swapChain = SwapChainFactory.CreateSwapChain(width, height, frameCount, context, hwnd);
        rtvHeap = CreateRTVHeap(context.Device, frameCount);

        viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
        scissor = new RectI(0, 0, width, height);
    }

    private ID3D12DescriptorHeap CreateRTVHeap(ID3D12Device device, uint frameCount)
    {
        return device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.RenderTargetView,
            frameCount,
            DescriptorHeapFlags.None,
            0));
    }


    public void BindForCommandList(ID3D12GraphicsCommandList cmd)
    {
        cmd.RSSetViewport(viewport);
        cmd.RSSetScissorRect(scissor);
    }

    public void Present()
    {
        swapChain.Present(1, PresentFlags.None);
    }

    public void Dispose()
    {
        swapChain?.Dispose();
        rtvHeap?.Dispose();
    }

    public ID3D12Resource GetBuffer(uint i)
    {
        return swapChain.GetBuffer<ID3D12Resource>(i);
    }

    public ResourceDescriptor GetRenderTargetView(uint i, ID3D12Device device)
    {

        var rtvDescriptorSize = swapChain.GetDevice<ID3D12Device>().GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        
        
        CpuDescriptorHandle rtvHeapCpuStart = rtvHeap.GetCPUDescriptorHandleForHeapStart();
        GpuDescriptorHandle rtvHeapGpuStart = rtvHeap.GetGPUDescriptorHandleForHeapStart();

        var rtvHandle = new CpuDescriptorHandle(in rtvHeapCpuStart, (int)i, rtvDescriptorSize);
        var renderTarget = swapChain.GetBuffer<ID3D12Resource>(i);
        device.CreateRenderTargetView(renderTarget, null, rtvHandle);

        return new( 
            rtvHandle,
            new (in rtvHeapGpuStart, (int)i, rtvDescriptorSize));
    }
}