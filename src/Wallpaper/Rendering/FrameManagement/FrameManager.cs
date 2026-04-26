using Renderer.Core;
using Renderer.Descriptors;
using Renderer.Resources;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Renderer.FrameManagement;


//Class which manage Frame work flow
public sealed class FrameManager : IDisposable
{
    private const int frameCount = 2;

    // D3D12
    private readonly IDXGISwapChain3 swapChain;
    private readonly ID3D12DescriptorHeap rtvHeap;


    //Frame Resource
    private FrameResource[] frameResources = new FrameResource[frameCount];

    // Synchronization
    private readonly ID3D12Fence fence;
    private ulong fenceValue;
    private readonly AutoResetEvent fenceEvent = new(false);

    private readonly GraphicsContext context;

    private readonly IntPtr hwnd;
    private readonly int width;
    private readonly int height;

    private readonly Viewport viewport;
    private readonly RectI scissor;
    private readonly HeapAllocator heap;
    private readonly FrameMetricManager manager;
    private readonly ConstantBufferRegistry constantBufferRegistry;

    public FrameManager(GraphicsContext context, IntPtr hwnd, int width, int height, HeapAllocator heap, ConstantBufferRegistry constantBufferRegistry)
    {
        this.hwnd = hwnd;
        this.width = width;
        this.height = height;
        this.heap = heap;

        this.context = context;
        var device = context.Device;

        manager = new FrameMetricManager(width, height);
        this.constantBufferRegistry = constantBufferRegistry;

        swapChain = SwapChainFactory.CreateSwapChain(width, height, frameCount, context, hwnd);

        fence = device.CreateFence(0);
        fenceValue = 1;

        rtvHeap = CreateRTVHeap(device);

        CreateFrameResources(device);

        viewport = new Viewport(0, 0, this.width, this.height, 0.0f, 1.0f);
        scissor = new RectI(0, 0, this.width, this.height);
    }

    private ID3D12DescriptorHeap CreateRTVHeap(ID3D12Device device)
    {
        return device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.RenderTargetView,
            frameCount,
            DescriptorHeapFlags.None,
            0));
    }

    private void CreateFrameResources(ID3D12Device device)
    {
        var _rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        CpuDescriptorHandle rtvHeapStart = rtvHeap.GetCPUDescriptorHandleForHeapStart();

        for (uint i = 0; i < frameCount; i++)
        {
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle(in rtvHeapStart, (int)i, _rtvDescriptorSize);

            var renderTarget = swapChain.GetBuffer<ID3D12Resource>(i);
            device.CreateRenderTargetView(renderTarget, null, rtvHandle);

            frameResources[i] = new FrameResource(i, device)
            {
                RenderTargetHandle = rtvHandle,
                RenderTarget = renderTarget,
                ConstantBindings = constantBufferRegistry.CreateFrameBindings(device)
            };
        }
    }

    public FrameResource BeginFrame()
    {
        var currentResource = frameResources[swapChain.CurrentBackBufferIndex];
        WaitForFrame(currentResource);
        currentResource.frameMetric = manager.Update();
        currentResource.CommandAllocator.Reset();

        var cmd = currentResource.CommandList;
        cmd.Reset(currentResource.CommandAllocator);

        // PRESENT -> RENDER_TARGET
        cmd.ResourceBarrierTransition(
            currentResource.RenderTarget,
            ResourceStates.Present,
            ResourceStates.RenderTarget);

        cmd.RSSetViewport(viewport);
        cmd.RSSetScissorRect(scissor);
        cmd.SetDescriptorHeaps(heap.Heap);

        cmd.OMSetRenderTargets(currentResource.RenderTargetHandle);
        cmd.ClearRenderTargetView(currentResource.RenderTargetHandle, new Color4(0.0f, 0.0f, 0.0f, 0.0f));
        return currentResource;
    }

    public void EndFrame(FrameResource currentResource)
    {
        // RENDER_TARGET -> PRESENT
        currentResource.CommandList.ResourceBarrierTransition(
            currentResource.RenderTarget,
            ResourceStates.RenderTarget,
            ResourceStates.Present);

        currentResource.CommandList.Close();

        context.CommandQueue.ExecuteCommandList(currentResource.CommandList);


        swapChain.Present(1, PresentFlags.None);

        ulong fenceValue = this.fenceValue;
        context.CommandQueue.Signal(fence, fenceValue);
        currentResource.FenceValue = fenceValue;
        this.fenceValue++;
    }

    private void WaitForFrame(FrameResource frame)
    {
        if (frame.FenceValue != 0 && fence.CompletedValue < frame.FenceValue)
        {
            fence.SetEventOnCompletion(
                frame.FenceValue,
                fenceEvent.SafeWaitHandle.DangerousGetHandle());

            fenceEvent.WaitOne();
        }
    }
    public void WaitForAllFrames()
    {
        for (int i = 0; i < frameCount; i++)
            WaitForFrame(frameResources[i]);
    }

    public void Dispose()
    {
        for (int i = 0; i < frameCount; i++)
        {
            frameResources[i].Dispose();
        }
        fence?.Dispose();
        fenceEvent?.Dispose();

        rtvHeap?.Dispose();
        swapChain?.Dispose();
    }
}