using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Models;
using Renderer.Commands;
using Renderer.Resources;
using SharpGLTF.Schema2;
using Vortice.Direct3D12;


static class ModelLoader
{
    static public Model loadModelFromGLTF(InitContext context, string relativepath)
    {
        var gltf = ModelRoot.Load(Path.Combine("resources", relativepath));

        TextureProvider textureProvider = new(context);

        var materialMap = ImportMaterials(context, textureProvider, gltf);
        var (meshMap, meshData) = ImportMeshes(context, gltf, materialMap);
        var nodes = ImportNodes(gltf, meshMap);

        return new Model()
        {
            Materials = materialMap,
            Meshes = meshMap,
            Nodes = nodes,
            MeshBuffer = meshData,
            
            RootNodes = ImportSceneRoots(gltf, nodes),
            textureProvider = textureProvider
        };
    }

    static private List<Models.Material> ImportMaterials(InitContext initContext, TextureProvider textureProvider, ModelRoot gltf)
    {
        return gltf.LogicalMaterials.Select(
            mat =>
            new Models.Material(initContext, textureProvider ,new MaterialDescription()
            {
                Name = mat.Name,
                DoubleSided = mat.DoubleSided,
                AlphaCutoff = mat.AlphaCutoff,
                AlphaMode = mat.Alpha switch
                {
                    AlphaMode.OPAQUE => "OPAQUE",
                    AlphaMode.MASK => throw new NotImplementedException(),
                    AlphaMode.BLEND => throw new NotImplementedException(),
                },
                BaseColorFactor = Vector4.One,
                BaseColorTexturePath = mat.Extras?["mmd_material"]?["texture_rel_path"]?.ToString()
            })
        ).ToList();
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

        vertexIndexRegistry.AddPrimitive(vertexCount, indexCount);
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

        var vertices = new StaticVertex[positions.Count];

        
        for (int i = 0; i < positions.Count; i++)
        {
            vertices[i] = new()
            {
                Position = positions[i],
                UV = texCoords?[i] ?? Vector2.Zero,
                Normal = normals?[i] ?? Vector3.Zero
            };
        };

        var indeces32 = primitive.GetIndices();
        if (indeces32.Any(x => x > ushort.MaxValue))
            throw new NotSupportedException("Only 16-bit indices are supported for now.");

        var (vertexView, indexView) = vertexIndexRegistry.UploadPrimitive(primitiveId++, vertices, indeces32.Select(x=>(ushort)x).ToArray());

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


    static private List<Models.Node> ImportNodes(ModelRoot gltf, List<Models.Mesh> meshMap)
    {
        var list = gltf.LogicalNodes.Select(node =>
            new Models.Node()
            {
                Name = node.Name ?? "",
                LocalTransform = node.LocalMatrix,
                Mesh = node.Mesh != null ? meshMap?[node.Mesh.LogicalIndex] : null
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