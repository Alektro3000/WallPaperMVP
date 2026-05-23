
using System.Numerics;
using System.Runtime.InteropServices;
using Models;
using Models.Material;
using ShaderConventions;
using SharpGen.Runtime;
using SharpGLTF.Schema2;
public class SkeletalPrimitiveLoader : PrimitiveLoader
{
    public SkeletalPrimitiveLoader(InitContext initContext, BindlessTextureProvider textureProvider) : base(initContext, textureProvider)
    {
    }

    public override RootSignatureDefinition GetRootSignatureDefinition()
    {
        if(rootSignatureDefinition != null)
            return rootSignatureDefinition;
        rootSignatureDefinition = new RootSignatureDefinition(InitContext, RootSignatureDefinitionType.SkeletalMesh, TextureProvider);
        return rootSignatureDefinition;
    }
    public override MaterialDefinition GetMaterialDefinition()
    {
        if(materialDefinition != null)
            return materialDefinition;
        materialDefinition = new MaterialDefinition(InitContext, GetRootSignatureDefinition(), "models\\materials\\pbr", new PermutationKey(true));
        return materialDefinition;
    }

    static private ulong PackVector(Vector4 vector4)
    {
        ulong mask = (1 << 16) - 1;
        var x = (ulong)vector4.X & mask;
        var y = (ulong)vector4.Y & mask;
        var z = (ulong)vector4.Z & mask;
        var w = (ulong)vector4.W & mask;
        return x | (y << 16) | (z << 32) | (w << 48);
    }

    public override int VertexSize {get => Marshal.SizeOf<SkeletalVertex>(); }

    public override LoadingPrimitiveDescription LoadPrimitive(MeshPrimitive primitive)
    {
        var positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();

        var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
        var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
        var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

        var joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
        var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

        var indeces32 = primitive.GetIndices();

        var bytes = new byte[positions.Count * VertexSize];

        Span<SkeletalVertex> vertexSpan =
                MemoryMarshal.Cast<byte, SkeletalVertex>(bytes.AsSpan());

        for (int i = 0; i < positions.Count; i++)
        {
            var ushortWeights = weights![i] * ushort.MaxValue;
            vertexSpan[i] = new SkeletalVertex
            {
                Position = positions[i],
                UV = texCoords?[i] ?? Vector2.Zero,
                Normal = normals?[i] ?? Vector3.Zero,
                Tangent = tangents?[i] ?? Vector4.Zero,
                packedJointIndices = PackVector(joints![i]),
                packedJointWeights = PackVector(ushortWeights)
            };
        }

        return new(bytes, positions.Count, indeces32, indeces32.Count);
    }
}