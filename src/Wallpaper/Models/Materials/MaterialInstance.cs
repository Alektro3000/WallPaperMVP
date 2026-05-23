using System.Numerics;
using System.Runtime.InteropServices;
using Renderer.FrameManagement;
using Renderer.Resources;

namespace Models;

public class MaterialInstance
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MaterialInfo
    {
        public Vector3 AlbedoColor;
        public int albedoTextureIndex;

        public int normalTextureIndex;
        
        public uint flags;
    }

    public Vector3? AlbedoColor;
    public Texture? AlbedoTexture;
    
    public Texture? NormalTexture;
    public BindlessTextureProvider TextureProvider { get; }

    private readonly ConstantBufferKey<MaterialInfo> ConstantKey;

    public MaterialInstance(
        InitContext initContext,
        BindlessTextureProvider textureProvider,
        MaterialDescription materialDescription)
    {
        TextureProvider = textureProvider;
        AlbedoColor = materialDescription.BaseColorFactor?.AsVector3();
        AlbedoTexture = materialDescription.BaseColorTexture;
        NormalTexture = materialDescription.NormalTexture;
        ConstantKey = initContext.ConstantBufferRegistry.Reserve<MaterialInfo>("Material Constant Buffer");
    }

    public void Bind(FrameResource frameResource)
    {
        var cmd = frameResource.CommandList;
        ref var materialConstantBuffer = ref frameResource.GetBufferConstantRef(ConstantKey);

        var settings = frameResource.Settings.GetSettings<Settings>();
        var flags = (AlbedoTexture != null) ? 1u : 0;
        flags |= (AlbedoColor != null) ? 2u : 0;
        flags |= (settings.showUV > 0) ? 4u : 0;
        flags |= (settings.showNormal > 0) ? 8u : 0;
        materialConstantBuffer.flags = flags;
        materialConstantBuffer.AlbedoColor = AlbedoColor ?? Vector3.Zero;
        materialConstantBuffer.albedoTextureIndex = TextureProvider.GetOrCreateBindlessIndex(AlbedoTexture);
        materialConstantBuffer.normalTextureIndex = TextureProvider.GetOrCreateBindlessIndex(NormalTexture);

        cmd.SetGraphicsRootConstantBufferView(1, frameResource.GetGPUVirtualAddress(ConstantKey));
    }
}
