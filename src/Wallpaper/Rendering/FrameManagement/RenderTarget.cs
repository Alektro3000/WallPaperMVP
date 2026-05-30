using Renderer.Core;
using Renderer.Descriptors;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Renderer.FrameManagement;
public sealed class RenderTarget : IDisposable
{
    private readonly ResourceDescriptor descriptor;
    private readonly ID3D12Resource resource;

    public RenderTarget(GraphicsContext context, int width, int height, ResourceDescriptor resourceDescriptor)
    {
        var device = context.Device;
        descriptor = resourceDescriptor;
        resource = CreateDepthBuffer(device, width, height);
        
    }
    private const Format DepthFormat = Format.D32_Float;

    
    public static ID3D12Resource CreateDepthBuffer(
        ID3D12Device device,
        int width,
        int height)
    {
        var depthDesc = ResourceDescription.Texture2D(
            DepthFormat,
            (uint)width,
            (uint)height,
            arraySize: 1,
            mipLevels: 1,
            sampleCount: 1,
            sampleQuality: 0,
            flags: ResourceFlags.AllowDepthStencil);

        var clearValue = new ClearValue
        {
            Format = DepthFormat,
            DepthStencil = new DepthStencilValue(1.0f, 0)
        };

        return device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            depthDesc,
            ResourceStates.DepthWrite,
            clearValue);
    }

    public void Dispose()
    {
        resource?.Dispose();
    }
}