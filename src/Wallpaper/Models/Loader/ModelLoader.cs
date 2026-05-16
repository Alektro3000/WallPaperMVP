using System.Numerics;
using Models;
using SharpGLTF.Schema2;


static class ModelLoader
{
    static public Model loadModelFromGLTF(InitContext context, string path, string name)
    {
        var absolute = Path.Combine("resources", path);
        var gltf = ModelRoot.Load(Path.Combine(absolute, name));

        TextureProvider textureProvider = new(context, absolute);

        var materialMap = ImportMaterials(context, textureProvider, gltf);
        var (meshMap, meshData) = ImportMeshes(context, gltf, materialMap);

        var nodes = ImportNodes(gltf, meshMap);

        var skins = ImportSkins(context, gltf, nodes);

        return new Model(context)
        {
            Materials = materialMap,
            Meshes = meshMap,
            Nodes = nodes,
            MeshBuffer = meshData,
            Skins = skins,
            
            RootNodes = ImportSceneRoots(gltf, nodes),
            textureProvider = textureProvider
        };
    }

    static private List<Models.Material> ImportMaterials(InitContext initContext, TextureProvider textureProvider, ModelRoot gltf)
    {
        return gltf.LogicalMaterials.Select(
            mat =>
            new Models.Material(initContext, new MaterialDescription()
            {
                Name = mat.Name,
                DoubleSided = mat.DoubleSided,
                AlphaCutoff = mat.AlphaCutoff,
                AlphaMode = mat.Alpha switch
                {
                    AlphaMode.OPAQUE => "OPAQUE",
                    AlphaMode.MASK => "OPAQUE", //throw new NotImplementedException(),
                    AlphaMode.BLEND => "OPAQUE", //throw new NotImplementedException(),
                },
                BaseColorFactor = Vector4.One,
                BaseColorTexture = GetBaseColorTexture(textureProvider, mat),
                NormalTexture = textureProvider.GetTextureFromGltfTexture(mat.FindChannel("Normal")?.Texture)
            })
        ).ToList();
    }

    static private Models.Texture? GetBaseColorTexture(TextureProvider textureProvider, SharpGLTF.Schema2.Material mat)
    {
        var mmdTexture = textureProvider.GetTextureFromFile(mat.Extras?["mmd_material"]?["texture_rel_path"]?.ToString());
        if(mmdTexture != null)
            return mmdTexture;

        var gltfTexture = mat.FindChannel("BaseColor")?.Texture;
        return textureProvider.GetTextureFromGltfTexture(gltfTexture);
    }

    static private (List<Models.Mesh>, MeshBuffer meshData) ImportMeshes(InitContext context, ModelRoot gltf, List<Models.Material> materialMap)
    {
        //Register Primitive Sizes
        var vertexIndexRegistry = new VertexIndexRegistry(context);
        foreach (var mesh in gltf.LogicalMeshes)
            foreach (var prim in mesh.Primitives)
            {
                RegisterPrimitive(prim, vertexIndexRegistry);
            }

        //Create buffer
        vertexIndexRegistry.CreateBuffer();

        //Bind Buffer Views and upload Data
        int primitiveId = 0;
        var meshes = gltf.LogicalMeshes.Select(
            mesh =>
            new Models.Mesh()
            {
                Name = mesh.Name,
                constantBufferKey = context.ConstantBufferRegistry.Reserve<Matrix4x4>("Mesh Constant Buffer"),
                Primitives = mesh.Primitives.Select(prim => LoadPrimitive(prim, materialMap, vertexIndexRegistry, ref primitiveId)).ToList()
            }
        ).ToList();

        return (meshes, new MeshBuffer(vertexIndexRegistry, context));
    }

    static private void RegisterPrimitive(MeshPrimitive primitive, VertexIndexRegistry vertexIndexRegistry)
    {
        if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
            throw new NotSupportedException("Only triangle primitives are supported for now.");

        int vertexCount = primitive.GetVertexAccessor("POSITION").Count;
        int indexCount = primitive.GetIndexAccessor().Count;
        int indexSize = Math.Max(primitive.GetIndexAccessor().Format.ByteSize, 2);

        vertexIndexRegistry.AddPrimitive(vertexCount, indexCount, indexSize);
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

    static private Primitive LoadPrimitive(
        MeshPrimitive primitive, 
        List<Models.Material> materialMap,
        VertexIndexRegistry vertexIndexRegistry, 
        ref int primitiveId)
    {
        if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
            throw new NotSupportedException("Only triangle primitives are supported for now.");


        var positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();

        var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
        var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

        var joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
        var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

        var vertices = new StaticVertex[positions.Count];

        
        for (int i = 0; i < positions.Count; i++)
        {
            var UshortWeights = (weights?[i] ?? Vector4.Zero) * ushort.MaxValue;

            vertices[i] = new()
            {
                Position = positions[i],
                UV = texCoords?[i] ?? Vector2.Zero,
                Normal = normals?[i] ?? Vector3.Zero,
                packedJointIndices = PackVector(joints?[i] ?? Vector4.Zero),
                packedJointWeights = PackVector(UshortWeights)
            };
        };

        var indeces32 = primitive.GetIndices();

        var (vertexView, indexView) = vertexIndexRegistry.UploadPrimitive(
            primitiveId++, 
            vertices, 
            indeces32);

        Models.Material? material = null;
        if (primitive.Material != null)
        {
            int materialId = primitive.Material.LogicalIndex;
            if (0 <= materialId && materialId < materialMap.Count)
                material = materialMap[materialId];
        }

        return new Primitive()
        {
            Material = material,
            VertexBufferView = vertexView,
            VertexCount = vertices.Length,
            IndexBufferView = indexView,
            IndexCount = indeces32.Count,
        };
    }

    static private List<Models.Skin> ImportSkins(InitContext context, ModelRoot gltf, List<Models.Node> nodeMap)
    {
        var skins = gltf.LogicalSkins.Select(x => new Models.Skin(context,
            x.Joints.Select(j => nodeMap[j.LogicalIndex]).ToArray(),
            x.InverseBindMatrices.ToArray()
        )).ToList();

        foreach(var node in gltf.LogicalNodes)
        {
            var index = node.Skin?.LogicalIndex;
            if(index != null)
                nodeMap[node.LogicalIndex].Skin = skins[(int)index];
        }
        return skins;
    }
    static private List<Models.Node> ImportNodes(ModelRoot gltf, List<Models.Mesh> meshMap)
    {
        var list = gltf.LogicalNodes.Select(node =>
            new Models.Node()
            {
                Name = node.Name ?? "",
                LocalTransform = node.LocalMatrix,
                Mesh = node.Mesh != null ? meshMap?[node.Mesh.LogicalIndex] : null,
            }
        ).ToList();

        foreach (var node in gltf.LogicalNodes)
        {
            var dstNode = list[node.LogicalIndex];

            dstNode.Children = node.VisualChildren
                .Select(x => list[x.LogicalIndex])
                .ToList();

            if (node.VisualParent != null)
            {
                dstNode.Parent = list[node.VisualParent.LogicalIndex];
            }
        }

        return list;
    }
    static private List<Models.Node> ImportSceneRoots(
        ModelRoot gltf,
        List<Models.Node> nodes)
    {
        var scene = gltf.DefaultScene ?? gltf.LogicalScenes.FirstOrDefault();

        if (scene == null)
            return new List<Models.Node>();

        return scene.VisualChildren
            .Select(root => nodes[root.LogicalIndex])
            .ToList();
    }
}