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
        public int packedTextureIndex;
        
        public uint flags;
	    public float Metallic = 0;
	    public float Roughness = 1;
	    public float AmbientIntensity = 0.03f;

        public MaterialInfo()
        {
        }

    }

    public Vector3? AlbedoColor;
    public Texture? AlbedoTexture;
    public bool Visible = true;
    
    public Texture? NormalTexture;
    public Texture? PackedTexture;
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
        PackedTexture = materialDescription.PackedTexture;
        ConstantKey = initContext.ConstantBufferRegistry.Reserve<MaterialInfo>("Material Constant Buffer");
    }

    public void Bind(FrameResource frameResource)
    {
        var cmd = frameResource.CommandList;
        ref var materialConstantBuffer = ref frameResource.GetBufferConstantRef(ConstantKey);

        var materialInfo = new MaterialInfo();

        var settings = frameResource.Settings.GetSettings<Settings>();
        var flags = (AlbedoTexture != null) ? 1u : 0;
        flags |= (NormalTexture != null) ? 2u : 0;
        flags |= (PackedTexture != null) ? 4u : 0;
        flags |= (settings.showNormal > 0) ? 8u : 0;
        materialInfo.flags = flags;
        materialInfo.AlbedoColor = AlbedoColor ?? Vector3.Zero;
        materialInfo.albedoTextureIndex = TextureProvider.GetOrCreateBindlessIndex(AlbedoTexture);
        materialInfo.normalTextureIndex = TextureProvider.GetOrCreateBindlessIndex(NormalTexture);
        materialInfo.packedTextureIndex = TextureProvider.GetOrCreateBindlessIndex(PackedTexture);
        materialInfo.Metallic = settings.metallic;
        materialInfo.Roughness = settings.roughness;
        materialInfo.AmbientIntensity = settings.ambientIntensity;

        materialConstantBuffer = materialInfo;

        cmd.SetGraphicsRootConstantBufferView(1, frameResource.GetGPUVirtualAddress(ConstantKey));
    }
}
