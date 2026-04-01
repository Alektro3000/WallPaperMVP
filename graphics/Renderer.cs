using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.Dxc;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D12.D3D12;
using FeatureLevel = Vortice.Direct3D.FeatureLevel;

public sealed class Renderer : IDisposable
{
    private const int FrameCount = 2;

    private readonly IntPtr _hwnd;
    private readonly int _width;
    private readonly int _height;

    // D3D12
    private ID3D12Device _device;
    private ID3D12CommandQueue _commandQueue;
    private ID3D12GraphicsCommandList _commandList;
    private IDXGISwapChain3 _swapChain;
    private ID3D12DescriptorHeap _rtvHeap;
    private uint _rtvDescriptorSize;


    //Frame Resource
    private FrameResource[] _frameResources = new FrameResource[FrameCount];


    // Synchronization
    private ID3D12Fence _fence;
    private ulong _fenceValue;
    private readonly AutoResetEvent _fenceEvent = new(false);

    //SubClasses
    private Renderer2D render2d;
    private ComputePass computeSubsystem;
    private GraphicPass graphicSubsystem;
    private ParticleBuffers particelSystem;

    public float time = 0f;

    //Compiler can't view subfunctions
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    public Renderer(IntPtr hwnd, int width, int height)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    {
        _hwnd = hwnd;
        _width = width;
        _height = height;

#if DEBUG
        if (D3D12GetDebugInterface<ID3D12Debug>() is ID3D12Debug debug)
        {
            debug.EnableDebugLayer();
        }
#endif

        CreateDevice();
        _commandQueue = _device!.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
        using var commandList = new ImmidiateCommandList(_device!, _commandQueue!);

        CreateSwapChain();
        CreateFrameResources();
        CreateCommandObjects();
        CreateFence();
        particelSystem = new ParticleBuffers(_device, commandList);
        computeSubsystem = new ComputePass(_device, particelSystem);
        render2d = new Renderer2D(_device!, _commandQueue!, _frameResources.Select(x => x.RenderTarget).ToArray());
        graphicSubsystem = new GraphicPass(_device, commandList, particelSystem, width, height);
        
    }

    private void CreateDevice()
    {
        var hr = D3D12CreateDevice(null, FeatureLevel.Level_11_0, out _device!);
        if (hr.Failure || _device == null)
            throw new NotSupportedException("Failed to create D3D12 device.");

#if DEBUG
        if (_device.QueryInterfaceOrNull<ID3D12InfoQueue>() is ID3D12InfoQueue infoQueue)
        {
            infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
        }
#endif
    }

    private void CreateCommandObjects()
    {
        _commandList = _device.CreateCommandList<ID3D12GraphicsCommandList>(
            0,
            CommandListType.Direct,
            _frameResources[0].CommandAllocator,
            null);

        _commandList.Close();
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
            _commandQueue,
            _hwnd,
            swapChainDesc);

        _swapChain = tempSwapChain.QueryInterface<IDXGISwapChain3>();
    }

    private void CreateFence()
    {
        _fence = _device.CreateFence(0);
        _fenceValue = 1;
    }

    private void CreateFrameResources()
    {
        _rtvHeap = _device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.RenderTargetView,
            FrameCount,
            DescriptorHeapFlags.None,
            0));

        _rtvDescriptorSize = _device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        CpuDescriptorHandle heapStart = _rtvHeap.GetCPUDescriptorHandleForHeapStart();

        for (uint i = 0; i < FrameCount; i++)
        {
            var renderTarget = _swapChain.GetBuffer<ID3D12Resource>(i);
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle(in heapStart, (int)i, _rtvDescriptorSize);

            _device.CreateRenderTargetView(renderTarget, null, rtvHandle);

            _frameResources[i] = new FrameResource()
            {
                RenderTarget = renderTarget,
                CommandAllocator = _device.CreateCommandAllocator(CommandListType.Direct)
            };
            unsafe
            {
                (_frameResources[i].ConstantBuffer, _frameResources[i].ConstantBufferHeap) =
                    BufferHelper.CreateStaticBuffer(_device, out _frameResources[i].MappedConstants);
            }
        }
    }


    public void Render()
    {
        uint frameIndex = _swapChain.CurrentBackBufferIndex;

        var currentResource = _frameResources[frameIndex];
        WaitForFrame(currentResource);
        currentResource.CommandAllocator.Reset();

        _commandList.Reset(currentResource.CommandAllocator);

        // PRESENT -> RENDER_TARGET
        _commandList.ResourceBarrierTransition(
            _frameResources[frameIndex].RenderTarget,
            ResourceStates.Present,
            ResourceStates.RenderTarget);   

        CpuDescriptorHandle heapStart = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle(heapStart, (int)frameIndex, _rtvDescriptorSize);

        _commandList.OMSetRenderTargets(rtvHandle);
        _commandList.ClearRenderTargetView(rtvHandle, new Color4(0.1f, 0.1f, 0.3f, 1.0f));
        
        // Update static buffer
        time = (time + 0.166f);
        float t = (float)(Math.Sin(time * 0.2) * 0.5 + 0.5);
        

        unsafe
        {
            Win32.GetCursorPos(out Win32.POINT point);
            var MousePos = new Vector2(((float)point.X)/_width,(_height-(float)point.Y)/_height)*2 - new Vector2(1,1);
            currentResource.MappedConstants->MousePos = MousePos;
            currentResource.MappedConstants->TintColor = new Vector4(MousePos.X, MousePos.Y, 1.0f, 1.0f);
            currentResource.MappedConstants->DeltaTime = 0.166f;
            currentResource.MappedConstants->particleCount =  particelSystem._particleCount;
        }

        // Begin of Compute Pass
        computeSubsystem.DispatchParticles(_commandList, currentResource);

        graphicSubsystem.Render(_commandList, currentResource, particelSystem.WriteBuffer);

        _commandList.Close();

        _commandQueue.ExecuteCommandList(_commandList);

        render2d.Render(frameIndex);

        _swapChain.Present(1, PresentFlags.None);


        ulong fenceValue = _fenceValue;
        _commandQueue.Signal(_fence, fenceValue);
        _frameResources[frameIndex].FenceValue = fenceValue;
        _fenceValue++;

        particelSystem.SwapBuffers();
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

    public void Dispose()
    {
        for (int i = 0; i < FrameCount; i++)
            WaitForFrame(_frameResources[i]);

        graphicSubsystem.Dispose();
        computeSubsystem.Dispose();
        particelSystem.Dispose();
        render2d.Dispose();

        for (int i = 0; i < FrameCount; i++)
        {
            _frameResources[i].Dispose();
        }

        _fence?.Dispose();
        _fenceEvent?.Dispose();

        _rtvHeap?.Dispose();
        _swapChain?.Dispose();
        _commandList?.Dispose();
        _commandQueue?.Dispose();
        _device?.Dispose();
    }
}