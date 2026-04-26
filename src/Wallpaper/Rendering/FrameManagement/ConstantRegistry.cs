using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

public sealed class ConstantBufferRegistry
{
    private int count;
    private readonly List<Func<ID3D12Device, ConstantBinding>> factories = [];

    public int Count => count;

    public ConstantBufferKey Reserve(Func<ID3D12Device, ConstantBinding> factory)
    {
        factories.Add(factory);
        return new ConstantBufferKey(count++);
    }

    public ConstantBinding[] CreateFrameBindings(ID3D12Device device)
    {
        return factories.Select(x => x(device))
            .ToArray();
    }
}