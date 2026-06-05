using System.Numerics;
using SharpGLTF.Schema2;

namespace Models;

public sealed class MaterialInstanceLoader
{
    private readonly InitContext initContext;
    private readonly TextureLoader textureLoader;
    private readonly BindlessTextureProvider textureProvider;

    public MaterialInstanceLoader(
        InitContext initContext,
        TextureLoader textureLoader,
        BindlessTextureProvider textureProvider)
    {
        this.initContext = initContext;
        this.textureLoader = textureLoader;
        this.textureProvider = textureProvider;
    }

    public List<MaterialInstance> Import(ModelRoot gltf)
    {
        return gltf.LogicalMaterials.Select(
            mat =>
            {
                var description = new MaterialDescription()
                {
                    Name = mat.Name,
                    DoubleSided = mat.DoubleSided,
                    AlphaCutoff = mat.AlphaCutoff,
                    BaseColorFactor = GetBaseColor(mat),
                    BaseColorTexture = GetBaseColorTexture(mat),
                    NormalTexture = textureLoader.GetTextureFromGltfTexture(mat.FindChannel("Normal")?.Texture, Vortice.DXGI.Format.R8G8B8A8_UNorm),
                    PackedTexture = textureLoader.GetTextureFromGltfTexture(mat.FindChannel("MetallicRoughness")?.Texture, Vortice.DXGI.Format.R8G8B8A8_UNorm)
                };

                return new MaterialInstance(initContext, textureProvider, description);
            }
        ).ToList();
    }

    private Renderer.Resources.Texture? GetBaseColorTexture(SharpGLTF.Schema2.Material mat)
    {
        var mmdTexture = textureLoader.GetTextureFromFile(mat.Extras?["mmd_material"]?["texture_rel_path"]?.ToString(), Vortice.DXGI.Format.R8G8B8A8_UNorm_SRgb);
        if (mmdTexture != null)
            return mmdTexture;

        var gltfTexture = mat.FindChannel("BaseColor")?.Texture;
        return textureLoader.GetTextureFromGltfTexture(gltfTexture, Vortice.DXGI.Format.R8G8B8A8_UNorm_SRgb);
    }
    private Vector4? GetBaseColor(SharpGLTF.Schema2.Material mat)
    {
        return mat.FindChannel("BaseColor")?.Color;
    }
}
