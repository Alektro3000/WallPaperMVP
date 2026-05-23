using System.Numerics;
using System.Runtime.InteropServices;
using Models;
using SharpGLTF.Schema2;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;


static class ModelLoader
{
    static public Model loadModelFromGLTF(InitContext context, string path, string name)
    {
        var absolute = Path.Combine("resources", path);
        var gltf = ModelRoot.Load(Path.Combine(absolute, name));

        TextureLoader textureLoader = new(context, absolute);
        BindlessTextureProvider bindlessTextures = new(context);

        // var staticMaterialDefinition = new StaticMaterialDefinition(
        //     context,
        //     staticRootSignature,
        //     "models\\materials\\static.hlsl");


        PrimitiveLoaderRegistry primitiveLoaderRegistry = new(context, bindlessTextures);
        MaterialInstanceLoader materialLoader = new(
            context,
            textureLoader,
            bindlessTextures);

        var materialMap = materialLoader.Import(gltf);
        var (meshMap, meshData) = ImportMeshes(context, gltf, materialMap, primitiveLoaderRegistry);


        var nodes = ImportNodes(gltf, meshMap);
        var animations = ImportAnimations(gltf, nodes);

        var skins = ImportSkins(context, gltf, nodes);

        var CameraTransform = nodes.FirstOrDefault(n => n.Name == "Camera")?.DefaultTransform ?? AffineTransform.Identity; 

        return new Model(context)
        {
            Materials = materialMap.ToList(),
            MaterialDefinitions = primitiveLoaderRegistry.GetMaterialDefinition(),
            RootSignatureDefinitions = primitiveLoaderRegistry.GetRootSignatureDefinitions(),
            Meshes = meshMap,
            Nodes = nodes,
            MeshBuffer = meshData,
            Skins = skins,
            Animations = animations,

            RootNodes = ImportSceneRoots(gltf, nodes),
            TextureLoader = textureLoader,
            CameraTransform = CameraTransform
        };
    }

    private static List<Models.Animation> ImportAnimations(ModelRoot gtlf, List<Models.Node> nodeMap)
    {
        return gtlf.LogicalAnimations.Select(
            x => new Models.Animation(x.Name,
                x.Channels.Max(x => x.GetRotationSampler()?.GetLinearKeys()?.LastOrDefault().Key) ?? 1,
                 x.Channels
                .GroupBy(node => node.TargetNode)
                .Select(node => ImportAnimationNode(gtlf, node.Key, node.ToArray(), nodeMap))
                .ToList())
        ).ToList();
    }

    public static AnimationNode ImportAnimationNode(ModelRoot gltf, SharpGLTF.Schema2.Node node, AnimationChannel[] animationChannels, List<Models.Node> nodeMap)
    {
        AnimationNode nodeAnimation = new()
        {
            Node = nodeMap[node.LogicalIndex]
        };

        foreach (var animationChannel in animationChannels)
        {
            switch (animationChannel.TargetNodePath)
            {
                case PropertyPath.translation:
                    foreach (var key in animationChannel.GetTranslationSampler().GetLinearKeys())
                    {
                        nodeAnimation.Translations.Add(
                            new LinearKey<Vector3>(key.Key, key.Value)
                        );
                    }
                    break;

                case PropertyPath.rotation:
                    foreach (var key in animationChannel.GetRotationSampler().GetLinearKeys())
                    {
                        nodeAnimation.Rotations.Add(
                            new LinearKey<Quaternion>(key.Key, key.Value)
                        );
                    }
                    break;

                case PropertyPath.scale:
                    foreach (var key in animationChannel.GetScaleSampler().GetLinearKeys())
                    {
                        nodeAnimation.Scales.Add(
                            new LinearKey<Vector3>(key.Key, key.Value)
                        );
                    }
                    break;

                case PropertyPath.weights:
                    // Morph target / shape key animation.
                    // Handle separately if you need blendshapes.
                    break;
            }
        }


        return nodeAnimation;
    }

    static private (List<Models.Mesh>, MeshBuffer meshData) ImportMeshes(InitContext context, ModelRoot gltf, List<MaterialInstance> materialMap, PrimitiveLoaderRegistry primitiveLoaderRegistry)
    {
        //Register Primitive Sizes
        var vertexIndexRegistry = new VertexIndexRegistry(context);
        foreach (var mesh in gltf.LogicalMeshes)
            foreach (var prim in mesh.Primitives)
            {
                RegisterPrimitive(prim, vertexIndexRegistry, primitiveLoaderRegistry);
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

    static private void RegisterPrimitive(MeshPrimitive primitive, VertexIndexRegistry vertexIndexRegistry, PrimitiveLoaderRegistry primitiveLoaderRegistry)
    {
        if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
            throw new NotSupportedException("Only triangle primitives are supported for now.");

        int vertexCount = primitive.GetVertexAccessor("POSITION").Count;
        int indexCount = primitive.GetIndexAccessor().Count;
        int indexSize = Math.Max(primitive.GetIndexAccessor().Format.ByteSize, 2);

        var loader = primitiveLoaderRegistry.GetPrimitiveLoader(primitive);

        vertexIndexRegistry.AddPrimitive(vertexCount, indexCount, indexSize, loader.VertexSize, loader);
    }


    static private Primitive LoadPrimitive(
        MeshPrimitive primitive,
        List<MaterialInstance> materialMap,
        VertexIndexRegistry vertexIndexRegistry,
        ref int primitiveId)
    {
        if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
            throw new NotSupportedException("Only triangle primitives are supported for now.");


        var currentPrimitiveId = primitiveId++;
        VertexBufferView vertexView;
        IndexBufferView indexView;

        var primitiveLoader = vertexIndexRegistry.usedLoaders[currentPrimitiveId];
        var primitiveDescription = primitiveLoader.LoadPrimitive(primitive);

        (vertexView, indexView) = vertexIndexRegistry.UploadPrimitive(
            currentPrimitiveId,
            primitiveDescription.vertexes,
            primitiveDescription.indeces);


        Models.MaterialInstance? material = null;
        if (primitive.Material != null)
        {
            int materialId = primitive.Material.LogicalIndex;
            if (0 <= materialId && materialId < materialMap.Count)
                material = materialMap[materialId];
        }

        return new Primitive()
        {
            MaterialDefinition = primitiveLoader.GetMaterialDefinition(),
            Material = material,
            VertexBufferView = vertexView,
            VertexCount = primitiveDescription.vertexCount,
            IndexBufferView = indexView,
            IndexCount = primitiveDescription.indexCount,
        };
    }

    static private List<Models.Skin> ImportSkins(InitContext context, ModelRoot gltf, List<Models.Node> nodeMap)
    {
        var skins = gltf.LogicalSkins.Select(x => new Models.Skin(context,
            x.Joints.Select(j => nodeMap[j.LogicalIndex]).ToArray(),
            x.InverseBindMatrices.ToArray()
        )).ToList();

        foreach (var node in gltf.LogicalNodes)
        {
            var index = node.Skin?.LogicalIndex;
            if (index != null)
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
                DefaultTransform = new AffineTransform(
                    node.LocalTransform.Translation,
                    node.LocalTransform.Rotation,
                    node.LocalTransform.Scale),
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
