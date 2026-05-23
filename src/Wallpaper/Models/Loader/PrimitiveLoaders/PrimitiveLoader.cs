

using System.Numerics;
using System.Runtime.InteropServices;
using Models;
using Models.Material;
using SharpGen.Runtime;
using SharpGLTF.Schema2;

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
    protected MaterialDefinition? materialDefinition;

    protected InitContext InitContext;
    public PrimitiveLoader(InitContext initContext, BindlessTextureProvider textureProvider)
    {
        TextureProvider = textureProvider;
        InitContext = initContext;
    }
    public abstract RootSignatureDefinition GetRootSignatureDefinition();
    public abstract int VertexSize {get;}
    public abstract MaterialDefinition GetMaterialDefinition();
    public abstract LoadingPrimitiveDescription LoadPrimitive(MeshPrimitive primitive);
}