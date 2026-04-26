using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Particles.Shared.Global;

public class Buffers : IConstantBufferSet
{
    public ConstantBufferKey commonKey;
    public Buffers(ConstantBufferRegistry registry)
    {
        commonKey = registry.Reserve(device => BufferFactory.CreateConstantBuffer<ConstantBuffer>(device, "CommonBuffer"));
    }

    public void Dispose()
    {
        
    }

}