
using Renderer.Core;
using Renderer.Descriptors;
using Renderer.Resources;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

#if TRANSPARENT
using Vortice.DirectComposition;
using Vortice.Direct3D11;
using Vortice.Direct3D11on12;
using static Vortice.DirectComposition.DComp;
#endif


namespace Renderer.FrameManagement;

public static class SwapChainFactory
{
    public static IDXGISwapChain3 CreateSwapChain(int width, int height, uint frameCount, GraphicsContext context, nint hwnd)
    {

#if DEBUG
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