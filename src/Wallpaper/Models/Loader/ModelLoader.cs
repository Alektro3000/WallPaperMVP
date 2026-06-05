using System.Numerics;
using System.Runtime.InteropServices;
using Models;
using Models.Lights;
using SharpGLTF.Schema2;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;

namespace Models.Loader;
static class ModelLoader
{
    static public Model loadModelFromGLTF(InitContext initContext, string path, string name)
    {
        var absolute = Path.Combine("resources", path);
        ReadSettings settings = new ReadSettings()
        {
            Validation = SharpGLTF.Validation.ValidationMode.TryFix
        };
        var gltf = ModelRoot.Load(Path.Combine(absolute, name), settings);

        TextureLoader textureLoader = new(initContext, absolute);
        BindlessTextureProvider bindlessTextures = new(initContext);

        PrimitiveLoaderRegistry primitiveLoaderRegistry = new(initContext, bindlessTextures);
        MaterialInstanceLoader materialLoader = new(
            initContext,
            textureLoader,
            bindlessTextures);

        var materialMap = materialLoader.Import(gltf);
        var (meshMap, meshData) = ImportMeshes(initContext, gltf, materialMap, primitiveLoaderRegistry);


        var nodes = ImportNodes(gltf, meshMap);
        var animations = ImportAnimations(gltf, nodes);

        var skins = ImportSkins(initContext, gltf, nodes);

        var model = new Model(initContext)
        {
            Materials = materialMap.ToList(),
            MaterialDefinitions = primitiveLoaderRegistry.GetMaterialDefinitions(),
            RootSignatureDefinitions = primitiveLoaderRegistry.GetRootSignatureDefinitions(),
            Meshes = meshMap,
            Nodes = nodes,
            MeshBuffer = meshData,
            Skins = skins,
            Animations = animations,

            RootNodes = ImportSceneRoots(gltf, nodes),
            TextureLoader = textureLoader,
            Camera = ImportCamera(gltf, nodes),
            Lights = ImportLights(initContext, bindlessTextures, gltf, nodes)
        };
        model.PostInit();
        return model;
    }

    private static List<Models.Animation> ImportAnimations(ModelRoot gtlf, List<Models.Node> nodeMap)
    {
        return gtlf.LogicalAnimations.Select(
            x => new Models.Animation(x.Name,
                x.Channels.Max(x => x.GetRotationSampler()?.GetLinearKeys()?.LastOrDefault().Key) ?? 1,
                 x.Channels
                .GroupBy(node => node.TargetNode)
                .Select(node => ImportAnimationNode(node.Key, node.ToArray(), nodeMap))
                .ToList())
        ).ToList();
    }

    public static AnimationNode ImportAnimationNode(SharpGLTF.Schema2.Node node, AnimationChannel[] animationChannels, List<Models.Node> nodeMap)
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
                    nodeAnimation.Translations.AddRange(
                        animationChannel.GetTranslationSampler()
                        .GetLinearKeys()
                        .Select(key => new LinearKey<Vector3>(key.Key, key.Value)));
                    break;

                case PropertyPath.rotation:
                    nodeAnimation.Rotations.AddRange(
                            animationChannel.GetRotationSampler()
                            .GetLinearKeys()                    
                            .Select(key => new LinearKey<Quaternion>(key.Key, key.Value)));
                    break;

                case PropertyPath.scale:
                    nodeAnimation.Scales.AddRange(
                        animationChannel.GetScaleSampler()
                        .GetLinearKeys()
                        .Select(key => new LinearKey<Vector3>(key.Key, key.Value)));

                    break;

                case PropertyPath.weights:
                    // Morph target / shape key animation.
                    // TODO handle weights.
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
                constantBufferKey = context.ConstantBufferRegistry.Reserve<MeshConstantBuffer>("Mesh Constant Buffer"),
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


        MaterialInstance? material = null;
        if (primitive.Material != null)
        {
            int materialId = primitive.Material.LogicalIndex;
            if (0 <= materialId && materialId < materialMap.Count)
                material = materialMap[materialId];
        }


        return new Primitive()
        {
            MaterialDefinition = primitiveLoader.GetMaterialDefinition(primitive.Material),
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

    static private List<PrincipledLight> ImportLights(InitContext initContext, BindlessTextureProvider textureProvider, ModelRoot gltf, List<Models.Node> nodes)
    {
        return gltf.LogicalPunctualLights
            .Select(PrincipledLight (PunctualLight x) => 
            {
                if(x.LightType == PunctualLightType.Spot)
                    return new SpotLight(initContext, textureProvider)
                    {
                        OuterConeAngle = x?.OuterConeAngle ?? (float)Math.PI/2f,
                        InnerConeAngle = x?.InnerConeAngle ?? (float)Math.PI/2f,
                        Intensity = x?.Intensity ?? 10,
                        Color = x?.Color ?? Vector3.One,
                        Radius = x?.Range ?? 10,
                        Node = nodes[gltf.LogicalNodes
                        .FirstOrDefault(n=>n.PunctualLight == x)?.LogicalIndex 
                        ?? throw new Exception("Invalid node for light")]
                    };
                else //if(x.LightType == PunctualLightType.Point)
                    return new PointLight()
                    {
                        Intensity = x?.Intensity ?? 10,
                        Color = x?.Color ?? Vector3.One,
                        Radius = x?.Range ?? 10,
                        Node = nodes[gltf.LogicalNodes
                        .FirstOrDefault(n=>n.PunctualLight == x)?.LogicalIndex 
                        ?? throw new Exception("Invalid node for light")]
                    };
            }).ToList();
    }

    static private Models.Camera ImportCamera(ModelRoot gltf, List<Models.Node> nodes)
    {
        var gltfCamera = gltf.LogicalCameras.FirstOrDefault();
        
        var perspective = gltfCamera?.Settings as CameraPerspective;
        var cameraNode = nodes.FirstOrDefault(n => n.Name == "Camera") ?? 
                throw new Exception("No camera node found in the model.");

        var yfov = perspective?.VerticalFOV ?? 1f;
        var znear = perspective?.ZNear ?? 0.1f;
        var zfar = perspective?.ZFar ?? 100f;

        return new Models.Camera
        {
            Node = cameraNode,
            yfov = yfov,
            zfar = zfar,
            znear = znear
        };
    }
}
