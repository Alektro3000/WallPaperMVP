using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Particles.Shared.Global;

public class Buffers : IConstantBufferSet
{
    public FrameManager.ConstantKey commonKey;
    public Buffers(FrameManager manager)
    {
        commonKey = manager.ReserveBuffer();
    }

    public void Dispose()
    {
        
    }

    public void InitBuffers(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(commonKey,BufferFactory.CreateConstantBuffer<ConstantBuffer>(device, "CommonBuffer"));
    }
}