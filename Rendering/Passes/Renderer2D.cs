using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.Direct3D11on12;
using Vortice.Direct3D12;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using FeatureLevel = Vortice.Direct3D.FeatureLevel;
using static Vortice.Direct3D11on12.Apis;
using Vortice.DCommon;

class Renderer2DPass : IDisposable
{
    // D3D11On12 + D2D
    private ID3D11Device _d3d11Device;
    private ID3D11DeviceContext _d3d11Context;
    private ID3D11On12Device _d3d11On12Device;

    private ID2D1Factory1 _d2dFactory;
    private ID2D1Device _d2dDevice;
    private ID2D1DeviceContext _d2dContext;

    private IDWriteFactory _writeFactory;
    private IDWriteTextFormat _textFormat;
    private ID2D1SolidColorBrush _brush;

    private float _timeDelta = 0.016f * 4;
    private float _x = 0;
    private float _y = 0;

    public Renderer2DPass(
        ID3D12Device device,
        SharpGen.Runtime.IUnknown _commandQueue)
    {

        // Create 11On12 device on top of the existing D3D12 device/queue.
        var hr = D3D11On12CreateDevice(
            device,
            DeviceCreationFlags.BgraSupport,
            [FeatureLevel.Level_12_0],
            [_commandQueue],
            0,
            out _d3d11Device,
            out _d3d11Context,
            out _);


        if (hr.Failure)
            throw new InvalidOperationException($"D3D11On12CreateDevice failed: {hr}");

        _d3d11On12Device = _d3d11Device.QueryInterface<ID3D11On12Device>();

        _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>(Vortice.Direct2D1.FactoryType.SingleThreaded);

        using IDXGIDevice dxgiDevice = _d3d11Device.QueryInterface<IDXGIDevice>();
        _d2dDevice = _d2dFactory.CreateDevice(dxgiDevice);
        _d2dContext = _d2dDevice.CreateDeviceContext(DeviceContextOptions.None);

        _writeFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
        _textFormat = _writeFactory.CreateTextFormat("Segoe UI", 32.0f);
        _brush = _d2dContext.CreateSolidColorBrush(new Color4(1f, 1f, 1f, 1f));

    }

    public void InitBuffer(FrameResource frameResource)
    {
        // Wrap D3D12 back buffer so D3D11/D2D can render into it.
        var wrappedDesc = new Vortice.Direct3D11on12.ResourceFlags
        {
            BindFlags = BindFlags.RenderTarget
        };

        var backBuffer = _d3d11On12Device.CreateWrappedResource<ID3D11Resource>(
            frameResource.RenderTarget,
            wrappedDesc,
            ResourceStates.RenderTarget,
            ResourceStates.Present);

        using IDXGISurface surface = backBuffer.QueryInterface<IDXGISurface>();

        var bitmapProps = new BitmapProperties1(
            new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
            96,
            96,
            BitmapOptions.Target | BitmapOptions.CannotDraw);

        frameResource.WrappedBackBuffer = backBuffer;
        frameResource.D2DTarget = _d2dContext.CreateBitmapFromDxgiSurface(surface, bitmapProps);
    }


    public void Render(FrameResource resource)
    {
        // Acquire wrapped resource for D3D11/D2D work
        _d3d11On12Device.AcquireWrappedResources([resource.WrappedBackBuffer], 1);
        _d2dContext.Target = resource.D2DTarget;
        _d2dContext.BeginDraw();

        _d2dContext.DrawText(
            "Hello from wallpaper 👋",
            _textFormat,
            new Rect(
                100 + (_x < 1400f ? _x : 2800f - _x),
                100 + (_y < 900f ? _y : 1800f - _y),
                800,
                200),
            _brush);

        _x = (_x + 50 * _timeDelta) % 2800f;
        _y = (_y + 60 * _timeDelta) % 1800f;

        var result = _d2dContext.EndDraw();
        if (result.Failure)
        {
            Console.WriteLine("Wrong 2D render");
        }
        // This transitions wrapped resource from InState -> OutState
        _d3d11On12Device.ReleaseWrappedResources([resource.WrappedBackBuffer], 1);
        _d3d11Context.Flush();
    }
    public void Dispose()
    {

        _brush?.Dispose();
        _textFormat?.Dispose();
        _writeFactory?.Dispose();

        _d2dContext?.Dispose();
        _d2dDevice?.Dispose();
        _d2dFactory?.Dispose();

        _d3d11On12Device?.Dispose();
        _d3d11Context?.Dispose();
        _d3d11Device?.Dispose();
    }
}