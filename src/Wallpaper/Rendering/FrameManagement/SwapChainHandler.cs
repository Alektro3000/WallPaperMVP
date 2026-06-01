
using Renderer.Core;
using Renderer.Descriptors;
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


public sealed class SwapChainHandler : IDisposable
{
    private readonly IDXGISwapChain3 swapChain;
    private readonly ID3D12DescriptorHeap rtvHeap;
    private readonly Viewport viewport;
    private readonly RectI scissor;

    public uint CurrentBackBufferIndex => swapChain.CurrentBackBufferIndex;

    public SwapChainHandler(GraphicsContext context, IntPtr hwnd, int width, int height, uint frameCount)
    {
        swapChain = CreateSwapChain(width, height, frameCount, context, hwnd);
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
    
    public static IDXGISwapChain3 CreateSwapChain(int width, int height, uint frameCount, GraphicsContext context, nint hwnd)
    {

#if DEBUGDX
        using IDXGIFactory4 factory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(true);
#else
        using IDXGIFactory4 factory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(false);
#endif

#if TRANSPARENT
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
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = Vortice.DXGI.AlphaMode.Premultiplied
        };
        using IDXGISwapChain1 tempSwapChain = factory.CreateSwapChainForComposition(
            context.CommandQueue,
            swapChainDesc);
        var swapChain = tempSwapChain.QueryInterface<IDXGISwapChain3>();

        Apis.D3D11On12CreateDevice(
            context.Device,
            DeviceCreationFlags.BgraSupport,
            [FeatureLevel.Level_12_0],
            [context.CommandQueue],
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
        dcompDevice.CreateTargetForHwnd(hwnd, true, out IDCompositionTarget target);

        // 4. Visual
        dcompDevice.CreateVisual(out IDCompositionVisual visual);

        // 5. Put swap chain into visual
        visual.SetContent(swapChain);

        // 6. Put visual into target
        target.SetRoot(visual);

        // 7. Apply
        dcompDevice.Commit();

        return swapChain;
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
}