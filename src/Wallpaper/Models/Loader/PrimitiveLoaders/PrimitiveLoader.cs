

using System.Numerics;
using System.Runtime.InteropServices;
using Models;
using Models.Material;
using SharpGen.Runtime;
using SharpGLTF.Schema2;

namespace Models.Loader;
public record struct LoadingPrimitiveDescription
(
    byte[] vertexes,
    int vertexCount,
    IList<uint> indeces,
    int indexCount
){};

public abstract class PrimitiveLoader
{
    protected BindlessTextureProvider TextureProvider { get; }
    protected RootSignatureDefinition? rootSignatureDefinition;
    protected Dictionary<MaterialPermutationKey, MaterialDefinition> materialDefinitions = new();


    protected InitContext InitContext;
    public PrimitiveLoader(InitContext initContext, BindlessTextureProvider textureProvider)
    {
        TextureProvider = textureProvider;
        InitContext = initContext;
    }
    public abstract RootSignatureDefinition GetRootSignatureDefinition();
    public abstract int VertexSize {get;}
    public List<MaterialDefinition> GetMaterialDefinitions() => materialDefinitions.Values.ToList();
    public abstract MaterialDefinition GetMaterialDefinition(SharpGLTF.Schema2.Material material);
    public abstract LoadingPrimitiveDescription LoadPrimitive(MeshPrimitive primitive);
}


public static class LoaderExtension
{
    public static AlphaMode ConvertAlphaMode(this SharpGLTF.Schema2.AlphaMode alphaMode)
    {
        return alphaMode switch
                    {
                        SharpGLTF.Schema2.AlphaMode.OPAQUE => AlphaMode.OPAQUE,
                        SharpGLTF.Schema2.AlphaMode.MASK => AlphaMode.MASK,
                        SharpGLTF.Schema2.AlphaMode.BLEND => AlphaMode.BLEND,
                        _ => throw new NotImplementedException("Unknown alpha mode"),
                    };
    }
} 