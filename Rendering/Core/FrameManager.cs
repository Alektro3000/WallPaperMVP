using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.DirectComposition;
using static Vortice.DirectComposition.DComp;
using Vortice.Direct3D11;
using Vortice.Direct3D11on12;
using Vortice.Direct3D;

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
    private ID3D12Fence _fence;
    private ulong _fenceValue;
    private readonly AutoResetEvent _fenceEvent = new(false);

    private GraphicsContext Context;

    private readonly IntPtr _hwnd;
    private readonly int _width;
    private readonly int _height;

    private Viewport _viewport;
    private RectI _scissor;
    private HeapAllocator _heap;
    private FrameMetricManager manager; 
    public FrameManager(GraphicsContext context, IntPtr hwnd, int width, int height, HeapAllocator heap)
    {
        _hwnd = hwnd;
        _width = width;
        _height = height;
        _heap = heap;

        Context = context;

        manager = new FrameMetricManager(width, height);

        CreateSwapChain();
        CreateFence(context.Device);
        CreateFrameResources(context.Device);

        _viewport = new Viewport(0, 0, _width, _height, 0.0f, 1.0f);
        _scissor = new RectI(0, 0, _width, _height);
    }

    private void CreateSwapChain()
    {

#if DEBUG
        using IDXGIFactory4 factory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(true);
#else
        using IDXGIFactory4 factory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(false);
#endif


        if(Transparent)
        {
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
                AlphaMode = AlphaMode.Premultiplied
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
        }
        else
        {
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
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore
            };
            using IDXGISwapChain1 tempSwapChain = factory.CreateSwapChainForHwnd(
                Context.CommandQueue,
                _hwnd,
                swapChainDesc);
            _swapChain = tempSwapChain.QueryInterface<IDXGISwapChain3>();
        }
    }

    private void CreateFence(ID3D12Device device)
    {
        _fence = device.CreateFence(0);
        _fenceValue = 1;
    }
    int constantBufferSize = 0;
    public ConstantKey ReserveBuffer()
    {
        return new ConstantKey(constantBufferSize++);
    }
    public void PopulateConstantBuffers()
    {
        foreach(var frame in FrameResources)
            frame.ConstantBindings = [.. Enumerable.Range(0, constantBufferSize).Select(x=>new FrameResource.ConstantBinding())];
    }
    private void CreateFrameResources(ID3D12Device device)
    {
        _rtvHeap = device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.RenderTargetView,
            FrameCount,
            DescriptorHeapFlags.None,
            0));

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
        var currentResource = FrameResources[ _swapChain.CurrentBackBufferIndex];
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

        cmd.RSSetViewport(_viewport);
        cmd.RSSetScissorRect(_scissor);
        cmd.SetDescriptorHeaps( _heap.Heap);

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

        ulong fenceValue = _fenceValue;
        Context.CommandQueue.Signal(_fence, fenceValue);
        currentResource.FenceValue = fenceValue;
        _fenceValue++;
    }

    public void ExecuteForEachFrame(Action<FrameResource> action)
    {
        for(int i = 0; i<FrameResources.Length; i++)
            action(FrameResources[i]);
    }

    private void WaitForFrame(FrameResource frame)
    {
        if (frame.FenceValue != 0 && _fence.CompletedValue < frame.FenceValue)
        {
            _fence.SetEventOnCompletion(
                frame.FenceValue,
                _fenceEvent.SafeWaitHandle.DangerousGetHandle());

            _fenceEvent.WaitOne();
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
        _fence?.Dispose();
        _fenceEvent?.Dispose();

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