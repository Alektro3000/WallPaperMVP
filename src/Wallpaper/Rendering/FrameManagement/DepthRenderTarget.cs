using Renderer.Core;
using Renderer.Descriptors;
using Renderer.Resources;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Renderer.FrameManagement;

public sealed class DepthRenderTarget : Texture
{
    public readonly ResourceDescriptor Descriptor;
    public readonly ID3D12Resource DepthResource;
    private readonly ID3D12DescriptorHeap dsvHeap;
    private readonly bool shaderReadable;

    private int Width;
    private int Height;

    private ID3D12DescriptorHeap CreateDSVHeap(ID3D12Device device)
    {
        return device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.DepthStencilView,
            1,
            DescriptorHeapFlags.None,
            0));
    }

    public DepthRenderTarget(GraphicsContext context, int width, int height, bool shaderReadable = false)
    {
        Width = width;
        Height = height;
        this.shaderReadable = shaderReadable;

        var device = context.Device;

        dsvHeap = CreateDSVHeap(device);
        Descriptor = new ResourceDescriptor(
            dsvHeap.GetCPUDescriptorHandleForHeapStart(),
            dsvHeap.GetGPUDescriptorHandleForHeapStart());

        DepthResource = CreateDepthBuffer(device, width, height, shaderReadable);


        device.CreateDepthStencilView(DepthResource,
            new DepthStencilViewDescription()
            {
                Format = Format.D32_Float,
                ViewDimension = DepthStencilViewDimension.Texture2D
            }, Descriptor.Cpu);
    }

    public string Name => "DepthMap";

    public ID3D12Resource TextureResource => DepthResource;

    public Format Format => shaderReadable ? Format.R32_Float : Format.D32_Float;

    int Texture.Width => Width;

    int Texture.Height => Height;

    public static ID3D12Resource CreateDepthBuffer(
        ID3D12Device device,
        int width,
        int height,
        bool shaderReadable = false)
    {
        var depthDesc = ResourceDescription.Texture2D(
            shaderReadable ? Format.R32_Typeless : Format.D32_Float,
            (uint)width,
            (uint)height,
            arraySize: 1,
            mipLevels: 1,
            sampleCount: 1,
            sampleQuality: 0,
            flags: ResourceFlags.AllowDepthStencil);

        ClearValue? clearValue = shaderReadable
            ? null
            : new ClearValue
            {
                Format = Format.D32_Float,
                DepthStencil = new DepthStencilValue(0.0f, 0)
            };

        return device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            depthDesc,
            shaderReadable ? ResourceStates.PixelShaderResource : ResourceStates.DepthWrite,
            clearValue);
    }

    public void Dispose()
    {
        DepthResource?.Dispose();
        dsvHeap?.Dispose();
    }
}
