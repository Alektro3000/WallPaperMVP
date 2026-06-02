
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Models;
using Models.Material;
using ShaderConventions;
using SharpGen.Runtime;
using SharpGLTF.Schema2;

namespace Models.Loader;
public class StaticPrimitiveLoader : PrimitiveLoader
{
    public StaticPrimitiveLoader(InitContext initContext, BindlessTextureProvider textureProvider) : base(initContext, textureProvider)
    {
    }

    public override RootSignatureDefinition GetRootSignatureDefinition()
    {
        if(rootSignatureDefinition != null)
            return rootSignatureDefinition;
        rootSignatureDefinition = new RootSignatureDefinition(InitContext, RootSignatureDefinitionType.StaticMesh, TextureProvider);
        return rootSignatureDefinition;
    }
    public override MaterialDefinition GetMaterialDefinition(SharpGLTF.Schema2.Material? material)
    {
        bool TwoSided = material?.DoubleSided ?? true;
        var permutationKey = new PermutationKey(false);
        var alpha = material?.Alpha.ConvertAlphaMode() ?? AlphaMode.OPAQUE;
        var mat = new MaterialPermutationKey(permutationKey, TwoSided, alpha);

        if(materialDefinitions.TryGetValue(mat, out var def))
        {
            return def;
        }
        var depthDef = new DepthDefinition(InitContext, GetRootSignatureDefinition(), "models\\materials\\pbr", mat);
        var matDef = new MaterialDefinition(InitContext, GetRootSignatureDefinition(), "models\\materials\\pbr", mat, depthDef);
        materialDefinitions[mat] = matDef;
        return matDef;
    }

    public override int VertexSize {get => Marshal.SizeOf<StaticMeshVertex>(); }

    public override LoadingPrimitiveDescription LoadPrimitive(MeshPrimitive primitive)
    {
        var positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();

        var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
        var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
        var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

        var indeces32 = primitive.GetIndices();

        var bytes = new byte[positions.Count * VertexSize];

        Span<StaticMeshVertex> vertexSpan =
                MemoryMarshal.Cast<byte, StaticMeshVertex>(bytes.AsSpan());

        for (int i = 0; i < positions.Count; i++)
        {
            vertexSpan[i] = new StaticMeshVertex
            {
                Position = positions[i],
                UV = texCoords?[i] ?? Vector2.Zero,
                Normal = normals?[i] ?? Vector3.Zero,
                Tangent = tangents?[i] ?? Vector4.UnitY,
            };
        }

        return new(bytes, positions.Count, indeces32, indeces32.Count);
    }
}