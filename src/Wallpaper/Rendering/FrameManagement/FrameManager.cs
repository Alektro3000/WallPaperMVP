using Renderer.Core;
using Renderer.Descriptors;
using Renderer.Resources;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

#if TRANSPARENT
using Vortice.DirectComposition;
using Vortice.Direct3D11;
using Vortice.Direct3D11on12;
using static Vortice.DirectComposition.DComp;
#endif


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

        swapChain = CreateSwapChain();
        
        fence = device.CreateFence(0);
        fenceValue = 1;
        
        rtvHeap = CreateRTVHeap(device);

        CreateFrameResources(device);

        viewport = new Viewport(0, 0, this.width, this.height, 0.0f, 1.0f);
        scissor = new RectI(0, 0, this.width, this.height);
    }

    private IDXGISwapChain3 CreateSwapChain()
    {

#if DEBUG
        using IDXGIFactory4 factory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(true);
#else
        using IDXGIFactory4 factory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(false);
#endif

#if TRANSPARENT
        var swapChainDesc = new SwapChainDescription1
        {
            Width = (uint)_width,
            Height = (uint)_height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = SampleDescription.Default,
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = FrameCount,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = Vortice.DXGI.AlphaMode.Premultiplied
        };
        using IDXGISwapChain1 tempSwapChain = factory.CreateSwapChainForComposition(
            Context.CommandQueue,
            swapChainDesc);
        _swapChain = tempSwapChain.QueryInterface<IDXGISwapChain3>();

        Apis.D3D11On12CreateDevice(
            Context.Device,
            DeviceCreationFlags.BgraSupport,
            [FeatureLevel.Level_12_0],
            [Context.CommandQueue],
            0,
            out ID3D11Device d3d11Device,
            out _,
            out _);

        IDXGIDevice dxgiDevice = d3d11Device.QueryInterface<IDXGIDevice>();

        // 2. DirectComposition device
        DCompositionCreateDevice(
            dxgiDevice,
            out IDCompositionDevice dcompDevice);

        // 3. Target bound to HWND
        dcompDevice.CreateTargetForHwnd(_hwnd, true, out IDCompositionTarget target);

        // 4. Visual
        dcompDevice.CreateVisual(out IDCompositionVisual visual);

        // 5. Put swap chain into visual
        visual.SetContent(_swapChain);

        // 6. Put visual into target
        target.SetRoot(visual);

        // 7. Apply
        dcompDevice.Commit();

#else
        
            var swapChainDesc = new SwapChainDescription1
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = SampleDescription.Default,
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = frameCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore
            };
            using IDXGISwapChain1 tempSwapChain = factory.CreateSwapChainForHwnd(
                context.CommandQueue,
                hwnd,
                swapChainDesc);
            return tempSwapChain.QueryInterface<IDXGISwapChain3>();
       
#endif 

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

            unsafe
            {

                frameResources[i] = new FrameResource(i, device)
                {

                    RenderTargetHandle = rtvHandle,
                    RenderTarget = renderTarget,
                    ConstantBindings = constantBufferRegistry.CreateFrameBindings(device)
                };
            }
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
        currentResource.CommandList.ResourceBarrierTransition(
            currentResource.RenderTarget,
            ResourceStates.RenderTarget,
            ResourceStates.Present);
            
        // RENDER_TARGET -> PRESENT
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