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
    private const int FrameCount = 2;
    private const bool Transparent = false;

    // D3D12
    private IDXGISwapChain3 _swapChain;
    private ID3D12DescriptorHeap _rtvHeap;


    //Frame Resource
    private FrameResource[] FrameResources = new FrameResource[FrameCount];

    public struct ConstantKey
    {
        internal readonly int key;

        internal ConstantKey(int key) : this()
        {
            this.key = key;
        }
    }

    // Synchronization
    private ID3D12Fence Fence;
    private ulong FenceValue;
    private readonly AutoResetEvent FenceEvent = new(false);

    private GraphicsContext Context;

    private readonly IntPtr HWND;
    private readonly int Width;
    private readonly int Height;

    private Viewport Viewport;
    private RectI Scissor;
    private HeapAllocator Heap;
    private FrameMetricManager Manager;

    public FrameManager(GraphicsContext context, IntPtr hwnd, int width, int height, HeapAllocator heap)
    {
        HWND = hwnd;
        Width = width;
        Height = height;
        Heap = heap;

        Context = context;
        var device = context.Device;

        Manager = new FrameMetricManager(width, height);

        _swapChain = CreateSwapChain();
        
        Fence = device.CreateFence(0);
        FenceValue = 1;
        
        _rtvHeap = CreateRTVHeap(device);

        CreateFrameResources(device);

        Viewport = new Viewport(0, 0, Width, Height, 0.0f, 1.0f);
        Scissor = new RectI(0, 0, Width, Height);
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
                Width = (uint)Width,
                Height = (uint)Height,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = SampleDescription.Default,
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = FrameCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore
            };
            using IDXGISwapChain1 tempSwapChain = factory.CreateSwapChainForHwnd(
                Context.CommandQueue,
                HWND,
                swapChainDesc);
            return tempSwapChain.QueryInterface<IDXGISwapChain3>();
       
#endif 
    }
    int constantBufferSize = 0;
    public ConstantKey ReserveBuffer()
    {
        return new ConstantKey(constantBufferSize++);
    }
    public void PopulateConstantBuffers()
    {
        foreach (var frame in FrameResources)
            frame.ConstantBindings = [.. Enumerable.Range(0, constantBufferSize).Select(x => new ConstantBinding())];
    }

    private ID3D12DescriptorHeap  CreateRTVHeap(ID3D12Device device)
    {
        return device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.RenderTargetView,
            FrameCount,
            DescriptorHeapFlags.None,
            0));
    }
    
    private void CreateFrameResources(ID3D12Device device)
    {
        var _rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        CpuDescriptorHandle rtvHeapStart = _rtvHeap.GetCPUDescriptorHandleForHeapStart();

        for (uint i = 0; i < FrameCount; i++)
        {
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle(in rtvHeapStart, (int)i, _rtvDescriptorSize);

            var renderTarget = _swapChain.GetBuffer<ID3D12Resource>(i);
            device.CreateRenderTargetView(renderTarget, null, rtvHandle);

            unsafe
            {

                FrameResources[i] = new FrameResource(i, device)
                {

                    RenderTargetHandle = rtvHandle,
                    RenderTarget = renderTarget,

                };
            }
        }
    }

    public FrameResource BeginFrame()
    {
        var currentResource = FrameResources[_swapChain.CurrentBackBufferIndex];
        WaitForFrame(currentResource);
        currentResource.frameMetric = Manager.Update();
        currentResource.CommandAllocator.Reset();

        var cmd = currentResource.CommandList;
        cmd.Reset(currentResource.CommandAllocator);

        // PRESENT -> RENDER_TARGET
        cmd.ResourceBarrierTransition(
            currentResource.RenderTarget,
            ResourceStates.Present,
            ResourceStates.RenderTarget);

        cmd.RSSetViewport(Viewport);
        cmd.RSSetScissorRect(Scissor);
        cmd.SetDescriptorHeaps(Heap.Heap);

        cmd.OMSetRenderTargets(currentResource.RenderTargetHandle);
        cmd.ClearRenderTargetView(currentResource.RenderTargetHandle, new Color4(0.0f, 0.0f, 0.0f, 0.0f));
        return currentResource;
    }

    public void ExecuteFrame(FrameResource currentResource)
    {
        // RENDER_TARGET -> PRESENT
        currentResource.CommandList.Close();

        Context.CommandQueue.ExecuteCommandList(currentResource.CommandList);
    }
    public void SwitchFrameResource(FrameResource currentResource)
    {
        currentResource.CommandList.ResourceBarrierTransition(
            currentResource.RenderTarget,
            ResourceStates.RenderTarget,
            ResourceStates.Present);
    }
    public void EndFrame(FrameResource currentResource)
    {
        SwitchFrameResource(currentResource);
        ExecuteFrame(currentResource);

        PresentFrame(currentResource);
    }
    public void PresentFrame(FrameResource currentResource)
    {
        _swapChain.Present(1, PresentFlags.None);

        ulong fenceValue = FenceValue;
        Context.CommandQueue.Signal(Fence, fenceValue);
        currentResource.FenceValue = fenceValue;
        FenceValue++;
    }

    public void ExecuteForEachFrame(Action<FrameResource> action)
    {
        for (int i = 0; i < FrameResources.Length; i++)
            action(FrameResources[i]);
    }

    private void WaitForFrame(FrameResource frame)
    {
        if (frame.FenceValue != 0 && Fence.CompletedValue < frame.FenceValue)
        {
            Fence.SetEventOnCompletion(
                frame.FenceValue,
                FenceEvent.SafeWaitHandle.DangerousGetHandle());

            FenceEvent.WaitOne();
        }
    }
    public void WaitForAllFrames()
    {
        for (int i = 0; i < FrameCount; i++)
            WaitForFrame(FrameResources[i]);
    }

    public void Dispose()
    {
        for (int i = 0; i < FrameCount; i++)
        {
            FrameResources[i].Dispose();
        }
        Fence?.Dispose();
        FenceEvent?.Dispose();

        _rtvHeap?.Dispose();
        _swapChain?.Dispose();
    }
    public void DrawFrame(Action<FrameResource> draw)
    {
        var frame = BeginFrame();
        draw(frame);
        PresentFrame(frame);
    }
}