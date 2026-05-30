using Renderer.Core;
using Renderer.Descriptors;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Renderer.FrameManagement;
public sealed class DepthRenderTarget : IDisposable
{
    public readonly ResourceDescriptor Descriptor;
    public readonly ID3D12Resource DepthResource;
    private readonly ID3D12DescriptorHeap dsvHeap;

    private ID3D12DescriptorHeap CreateDSVHeap(ID3D12Device device)
    {
        return device.CreateDescriptorHeap(new DescriptorHeapDescription(
            DescriptorHeapType.DepthStencilView,
            1,
            DescriptorHeapFlags.None,
            0));
    }

    public DepthRenderTarget(GraphicsContext context, int width, int height)
    {
        var device = context.Device;
        
        dsvHeap = CreateDSVHeap(device);
        Descriptor = new ResourceDescriptor(
            dsvHeap.GetCPUDescriptorHandleForHeapStart(),
            dsvHeap.GetGPUDescriptorHandleForHeapStart());

        DepthResource = CreateDepthBuffer(device, width, height);
        

        device.CreateDepthStencilView(DepthResource, null, Descriptor.Cpu);
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
        DepthResource?.Dispose();
        dsvHeap?.Dispose();
    }
}