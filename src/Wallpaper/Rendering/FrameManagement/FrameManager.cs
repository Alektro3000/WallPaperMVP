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


    // Depth
    private readonly ID3D12DescriptorHeap dsvHeap;
    private readonly ID3D12Resource depthBuffer;
    private readonly CpuDescriptorHandle depthStencilHandle;
    private const Format DepthFormat = Format.D32_Float;
    
    //Frame Resource
    private FrameResource[] frameResources = new FrameResource[frameCount];


    private readonly GraphicsContext context;

    private readonly int width;
    private readonly int height;

    private readonly Viewport viewport;
    private readonly RectI scissor;
    private readonly HeapAllocator heap;
    private readonly FrameMetricManager manager;
    private readonly ConstantBufferRegistry constantBufferRegistry;
    private readonly FrameCommandList frameCommandList;

    public FrameManager(GraphicsContext context, IntPtr hwnd, int width, int height, HeapAllocator heap, ConstantBufferRegistry constantBufferRegistry)
    {
        this.width = width;
        this.height = height;
        this.heap = heap;

        this.context = context;
        var device = context.Device;

        manager = new FrameMetricManager(width, height);
        this.constantBufferRegistry = constantBufferRegistry;

        swapChain = SwapChainFactory.CreateSwapChain(width, height, frameCount, context, hwnd);
        frameCommandList = new FrameCommandList(device);


        rtvHeap = CreateRTVHeap(device);

        dsvHeap = CreateDSVHeap(device);
        depthBuffer = CreateDepthBuffer(device, width, height);
        depthStencilHandle = dsvHeap.GetCPUDescriptorHandleForHeapStart();

        device.CreateDepthStencilView(depthBuffer, null, depthStencilHandle);

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
    private ID3D12DescriptorHeap CreateDSVHeap(ID3D12Device device)
    {
        return device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.DepthStencilView,
            1,
            DescriptorHeapFlags.None,
            0));
    }
    private static ID3D12Resource CreateDepthBuffer(
        ID3D12Device device,
        int width,
        int height)
    {
        var depthDesc = ResourceDescription.Texture2D(
            DepthFormat,
            (uint)width,
            (uint)height,
            arraySize: 1,
            mipLevels: 1,
            sampleCount: 1,
            sampleQuality: 0,
            flags: ResourceFlags.AllowDepthStencil);

        var clearValue = new ClearValue
        {
            Format = DepthFormat,
            DepthStencil = new DepthStencilValue(1.0f, 0)
        };

        return device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            depthDesc,
            ResourceStates.DepthWrite,
            clearValue);
    }


    private void CreateFrameResources(ID3D12Device device)
    {
        var rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        CpuDescriptorHandle rtvHeapStart = rtvHeap.GetCPUDescriptorHandleForHeapStart();

        for (uint i = 0; i < frameCount; i++)
        {
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle(in rtvHeapStart, (int)i, rtvDescriptorSize);

            var renderTarget = swapChain.GetBuffer<ID3D12Resource>(i);
            device.CreateRenderTargetView(renderTarget, null, rtvHandle);

            frameResources[i] = new FrameResource(device, constantBufferRegistry.CreateFrameBindings(device))
            {
                RenderTargetHandle = rtvHandle,
                RenderTarget = renderTarget,
                DepthStencilHandle = depthStencilHandle,
                DepthStencil = depthBuffer,
            };
        }
    }

    public FrameResource BeginFrame()
    {
        Serilog.Log.Debug("FrameManager: waiting for back buffer");
        var currentResource = frameResources[swapChain.CurrentBackBufferIndex];
        frameCommandList.WaitForFrame(currentResource);
        Serilog.Log.Debug("FrameManager: frame available");
        currentResource.FrameMetric = manager.Update();

        frameCommandList.ResetCmd(currentResource);
        var cmd = currentResource.CommandList;

        // PRESENT -> RENDER_TARGET
        cmd.ResourceBarrierTransition(
            currentResource.RenderTarget,
            ResourceStates.Present,
            ResourceStates.RenderTarget);

        cmd.RSSetViewport(viewport);
        cmd.RSSetScissorRect(scissor);
        heap.BindForCommandList(cmd);

        cmd.OMSetRenderTargets(currentResource.RenderTargetHandle, currentResource.DepthStencilHandle);

        cmd.ClearRenderTargetView(
            currentResource.RenderTargetHandle, 
            new Color4(0.0f, 0.0f, 0.0f, 0.0f));
            
        cmd.ClearDepthStencilView(
            depthStencilHandle,
            ClearFlags.Depth,
            1.0f,
            0);
        return currentResource;
    }

    public void UpdateFrameMetricOnly()
    {
        var currentResource = frameResources[swapChain.CurrentBackBufferIndex];
        currentResource.FrameMetric = manager.Update();
    }

    public void EndFrame(FrameResource currentResource)
    {
        Serilog.Log.Debug("FrameManager: closing and presenting frame {FrameIndex}", currentResource.FrameMetric.FrameIndex);
        // RENDER_TARGET -> PRESENT
        currentResource.CommandList.ResourceBarrierTransition(
            currentResource.RenderTarget,
            ResourceStates.RenderTarget,
            ResourceStates.Present);

        currentResource.CommandList.Close();

        context.CommandQueue.ExecuteCommandList(currentResource.CommandList);


        swapChain.Present(1, PresentFlags.None);

        frameCommandList.SetSignal(currentResource, context.CommandQueue);
        Serilog.Log.Debug("FrameManager: present completed for frame {FrameIndex}", currentResource.FrameMetric.FrameIndex);
    }

    public void Dispose()
    {
        for (int i = 0; i < frameCount; i++)
        {
            frameResources[i].Dispose();
        }

        frameCommandList?.Dispose();
        rtvHeap?.Dispose();
        swapChain?.Dispose();
    }

    public void WaitForAllFrames()
    {
        frameCommandList.WaitForAllFrames(frameResources);
    }
}
