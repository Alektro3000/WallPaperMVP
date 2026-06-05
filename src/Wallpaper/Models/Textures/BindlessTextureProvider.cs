using Renderer.Descriptors;
using Renderer.Resources;
using Renderer.Shaders;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Models;

public sealed class BindlessTextureProvider
{
    private const int BindlessCapacity = 128;

    private readonly ID3D12Device device;
    private readonly HeapAllocator allocator;
    private readonly Dictionary<Texture, int> textureToIndex = [];
    private readonly ResourceDescriptorRange bindlessRange;
    private readonly GpuDescriptorHandle bindlessTableStart;
    private int nextSlot;

    public BindlessTextureProvider(InitContext initContext)
    {
        device = initContext.GraphicsContext.Device;
        allocator = initContext.HeapAllocator;
        bindlessRange = allocator.Allocate(BindlessCapacity);
        bindlessTableStart = bindlessRange[0].Gpu;
        nextSlot = 0;
    }

    public int GetOrCreateBindlessIndex(Texture? texture)
    {
        if (texture == null)
            return -1;

        if (textureToIndex.TryGetValue(texture, out var index))
            return index;

        if (nextSlot >= BindlessCapacity)
            throw new InvalidOperationException($"Bindless texture capacity exceeded ({BindlessCapacity}).");

        index = nextSlot++;
        var descriptor = bindlessRange[index];
        CreateSrvForTexture(texture, descriptor);
        textureToIndex[texture] = index;
        return index;
    }

    public GpuDescriptorHandle GetBindlessTableStart()
    {
        return bindlessTableStart;
    }

    private void CreateSrvForTexture(Texture texture, ResourceDescriptor descriptor)
    {
        var srvDesc = new ShaderResourceViewDescription
        {
            Format = texture.Format,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Shader4ComponentMapping = ShaderConstants.Shader4ComponentMapping,
            Texture2D = new Texture2DShaderResourceView
            {
                MostDetailedMip = 0,
                MipLevels = 1,
                PlaneSlice = 0,
                ResourceMinLODClamp = 0.0f
            }
        };

        device.CreateShaderResourceView(texture.TextureResource, srvDesc, descriptor.Cpu);
    }
}
