using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Renderer.Resources;
public sealed class ConstantBufferRegistry
{
    private int count;
    private readonly List<Func<ID3D12Device, ConstantBinding>> factories = [];

    
    public ConstantBufferKey<T> Reserve<T>(String name) where T : unmanaged
    {
        factories.Add(device => BufferFactory.CreateConstantBuffer<T>(device, name));
        return new ConstantBufferKey<T>(count++);
    }
    public ConstantBinding[] CreateFrameBindings(ID3D12Device device)
    {
        return factories.Select(x => x(device))
            .ToArray();
    }
}