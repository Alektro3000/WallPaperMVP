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
                    AlphaMode = mat.Alpha switch
                    {
                        AlphaMode.OPAQUE => "OPAQUE",
                        AlphaMode.MASK => "OPAQUE",
                        AlphaMode.BLEND => "OPAQUE",
                    },
                    BaseColorFactor = GetBaseColor(mat),
                    BaseColorTexture = GetBaseColorTexture(mat),
                    NormalTexture = textureLoader.GetTextureFromGltfTexture(mat.FindChannel("Normal")?.Texture)
                };

                return new MaterialInstance(initContext, textureProvider, description);
            }
        ).ToList();
    }

    private Texture? GetBaseColorTexture(SharpGLTF.Schema2.Material mat)
    {
        var mmdTexture = textureLoader.GetTextureFromFile(mat.Extras?["mmd_material"]?["texture_rel_path"]?.ToString());
        if (mmdTexture != null)
            return mmdTexture;

        var gltfTexture = mat.FindChannel("BaseColor")?.Texture;
        return textureLoader.GetTextureFromGltfTexture(gltfTexture);
    }
    private Vector4? GetBaseColor(SharpGLTF.Schema2.Material mat)
    {
        return mat.FindChannel("BaseColor")?.Color;
    }
}
