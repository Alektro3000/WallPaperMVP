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

    
    //Frame Resource
    private FrameResource[] frameResources = new FrameResource[frameCount];


    private readonly GraphicsContext context;

    private readonly int width;
    private readonly int height;

    private readonly HeapAllocator heap;
    private readonly FrameMetricManager manager;
    private readonly DepthRenderTarget depthRenderTarget;
    private readonly SwapChainHandler screenRenderTarget;
    
    private readonly FrameCommandList frameCommandList;

    public FrameManager(GraphicsContext context, IntPtr hwnd, int width, int height, HeapAllocator heap, ConstantBufferRegistry constantBufferRegistry)
    {
        this.width = width;
        this.height = height;
        this.heap = heap;

        this.context = context;
        var device = context.Device;

        manager = new FrameMetricManager(width, height);

        frameCommandList = new FrameCommandList(device);
        depthRenderTarget = new DepthRenderTarget(context, width, height);
        screenRenderTarget = new SwapChainHandler(context, hwnd, width, height, frameCount);

        for(uint i = 0; i < frameCount; i++)
            frameResources[i] = new FrameResource(device, 
                constantBufferRegistry.CreateFrameBindings(device), 
                screenRenderTarget)
            {
                RenderTarget = screenRenderTarget.GetBuffer(i),
                RenderTargetHandle = screenRenderTarget.GetRenderTargetView(i, device),

                DepthStencil = depthRenderTarget.DepthResource,
                DepthStencilHandle = depthRenderTarget.Descriptor,
            };
    }


    public FrameResource BeginFrame()
    {
        Serilog.Log.Debug("FrameManager: waiting for back buffer");
        var currentResource = frameResources[screenRenderTarget.CurrentBackBufferIndex];
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

        heap.BindForCommandList(cmd);

        currentResource.BindRenderTarget();
        currentResource.ClearRenderTarget();

        return currentResource;
    }

    public void UpdateFrameMetricOnly()
    {
        var currentResource = frameResources[screenRenderTarget.CurrentBackBufferIndex];
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


        screenRenderTarget.Present();

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
        depthRenderTarget?.Dispose();
        screenRenderTarget?.Dispose();
    }

    public void WaitForAllFrames()
    {
        frameCommandList.WaitForAllFrames(frameResources);
    }
}
