using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

public sealed class FrameManager : IDisposable
{
    private const int FrameCount = 2;

    // D3D12
    private IDXGISwapChain3 _swapChain;
    private ID3D12DescriptorHeap _rtvHeap;
    private ID3D12DescriptorHeap _cbvHeap;


    //Frame Resource
    private FrameResource[] FrameResources = new FrameResource[FrameCount];


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
    public FrameManager(GraphicsContext context, IntPtr hwnd, int width, int height, HeapAllocator heap)
    {
        _hwnd = hwnd;
        _width = width;
        _height = height;
        _heap = heap;

        Context = context;

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

    private void CreateFence(ID3D12Device device)
    {
        _fence = device.CreateFence(0);
        _fenceValue = 1;
    }

    private void CreateFrameResources(ID3D12Device device)
    {
        _rtvHeap = device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.RenderTargetView,
            FrameCount,
            DescriptorHeapFlags.None,
            0));

        _cbvHeap = device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            FrameCount,
            DescriptorHeapFlags.ShaderVisible,
            0));

        var _rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        var _cbvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        CpuDescriptorHandle rtvHeapStart = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        CpuDescriptorHandle cbvheapStart = _cbvHeap.GetCPUDescriptorHandleForHeapStart();

        for (uint i = 0; i < FrameCount; i++)
        {
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle(in rtvHeapStart, (int)i, _rtvDescriptorSize);
            CpuDescriptorHandle cbvHandle = new CpuDescriptorHandle(in cbvheapStart, (int)i, _cbvDescriptorSize);

            var renderTarget = _swapChain.GetBuffer<ID3D12Resource>(i);
            device.CreateRenderTargetView(renderTarget, null, rtvHandle);

            unsafe
            {
                var constantBuffer = BufferHelper.CreateStaticBuffer(device, cbvHandle, out Constants* MappedConstants);

                FrameResources[i] = new FrameResource(i, device)
                {

                    RenderTargetHandle = rtvHandle,
                    RenderTarget = renderTarget,

                    ConstantBuffer = constantBuffer,
                    MappedConstants = MappedConstants,
                    ConstantHandle = cbvHandle,
                };
            }
        } 
    }

    public FrameResource BeginFrame()
    {
        var currentResource = FrameResources[ _swapChain.CurrentBackBufferIndex];
        WaitForFrame(currentResource);
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
        cmd.ClearRenderTargetView(currentResource.RenderTargetHandle, new Color4(0.1f, 0.1f, 0.3f, 1.0f));
        return currentResource;
    }

    public void ExecuteFrame(FrameResource currentResource)
    {
        // RENDER_TARGET -> PRESENT
        currentResource.CommandList.Close();

        Context.CommandQueue.ExecuteCommandList(currentResource.CommandList);
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