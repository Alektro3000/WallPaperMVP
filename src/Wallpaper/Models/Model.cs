using System.Numerics;
using System.Runtime.InteropServices;
using Models.Lights;
using Models.Material;
using Renderer.FrameManagement;
using Renderer.Resources;
using Settings;
using SharpGen.Runtime;
using Vortice.Direct3D12;

namespace Models;


public sealed class Model(InitContext initContext) : IDisposable
{
    public required Camera Camera;
    public List<MaterialInstance> Materials = [];
    public List<MaterialDefinition> MaterialDefinitions = [];
    public List<RootSignatureDefinition> RootSignatureDefinitions = [];
    public required MeshBuffer MeshBuffer;
    public List<Skin> Skins = [];
    public List<Mesh> Meshes = [];
    public List<Node> Nodes = [];
    public List<Node> RootNodes = [];
    public List<Animation> Animations = [];
    public List<PrincipledLight> Lights = [];
    public required TextureLoader TextureLoader;
    public float rotationDelta = 0;
    private ConstantBufferKey<SceneConstantBuffer> sceneConstantBufferKey =
        initContext.ConstantBufferRegistry.Reserve<SceneConstantBuffer>("MainPassSceneConstantBuffer");

    public List<MaterialGrouping> PrimitivesToRender = [];

    public void PostInit()
    {
        PrimitivesToRender =
            Nodes
            .Where(x => x.Mesh != null)
            .SelectMany(node => node?.Mesh?.Primitives?.Select(p => (node, p))?.ToList<(Node node, Primitive primitive)>() ?? [])
            .GroupBy(x => x.primitive.MaterialDefinition)
            .Select(materialGroup => new MaterialGrouping
            {
                MaterialDefinition = materialGroup.Key,
                Nodes = [.. materialGroup.GroupBy(x => x.node, x => x.primitive)]
            })
            .OrderBy(x => x.MaterialDefinition.alphaMode)
            .ToList();
    }
    public void Dispose()
    {
        Skins?.ForEach(x => x.Dispose());
        Meshes?.ForEach(x => x.Dispose());
        Lights?.ForEach(x => x.Dispose());
        TextureLoader.Dispose();

        MaterialDefinitions?.ForEach(x => x.Dispose());
        RootSignatureDefinitions?.ForEach(x => x.Dispose());

        MeshBuffer.Dispose();
    }

    public void Render(FrameResource frameResource)
    {
        updateNodePositions(frameResource);
        foreach (var i in Nodes.Where(x => x.Skin != null))
        {
            i.Skin?.UpdateJointsPositions(frameResource, i.GlobalMatrix);
        }

        var spotLightConstants = ProcessLight(frameResource);

        MainPass(frameResource, spotLightConstants);
    }
    public LightConstant[] ProcessLight(FrameResource frameResource)
    {
        var set = frameResource.Settings.GetSettings<Settings>();

        LightConstant[] spotLightConstants = new LightConstant[Math.Min(Lights.Count, 8)];

        for (int i = 0; i < spotLightConstants.Length; i++)
        {
            var light = Lights[i];
            var lightConstant = light.GetLightConstant();
            lightConstant.LightColor *= set.Intensity;

            light.BindRenderTarget(frameResource, 0);

            var sceneConstantBuffer = GenerateSceneConstantBuffer(
                frameResource, spotLightConstants, lightConstant.LightViewProjection, lightConstant.LightPosition);

            var sceneConstantBufferKey = light.GetSceneConstantKey(0);
            
            frameResource.GetBufferConstantRef(sceneConstantBufferKey) = sceneConstantBuffer;

            RenderMesh(frameResource, sceneConstantBufferKey, true);

            light.UnbindRenderTarget(frameResource, 0);

            spotLightConstants[i] = lightConstant;
        }

        return spotLightConstants;
    }

    public void MainPass(FrameResource frameResource, LightConstant[] spotLightConstants)
    {
        var set = frameResource.Settings.GetSettings<Settings>();

        if (set.showConcreteLight != -1)
        {
            spotLightConstants = spotLightConstants.Skip((int)set.showConcreteLight).Take(1).ToArray();
        }

        rotationDelta += frameResource.FrameMetric.DeltaTime * set.CameraSpeed;
        rotationDelta %= (float)(Math.PI * 2);

        var rotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), rotationDelta);

        var cameraPos = Camera.Node.GlobalTransform.Translation;
        var viewMatrix = Matrix4x4.Invert(Camera.Node.GlobalTransform.Matrix, out var inv)
            ? inv
            : Matrix4x4.Identity;

        if (true)
        {
            cameraPos = set.centerPos + Vector3.Transform(set.cameraPos, rotation);
            viewMatrix = Matrix4x4.CreateLookAt(
                cameraPosition: cameraPos,
                cameraTarget: set.centerPos,
                cameraUpVector: Vector3.UnitY);
        }

        var projection = Matrix4x4Ex.CreatePerspectiveFieldOfViewReversedZ(
            fieldOfView: Camera.yfov,
            aspectRatio: frameResource.FrameMetric.ratio,
            nearPlaneDistance: Camera.znear,
            farPlaneDistance: Camera.zfar
        );

        var vp = viewMatrix * projection;

        var cmd = frameResource.CommandList;

        ref var sceneConstantBuffer = ref frameResource.GetBufferConstantRef(sceneConstantBufferKey);
        if (set.useFreeCamera > 0)
        {
            var light = spotLightConstants[0];
            sceneConstantBuffer = GenerateSceneConstantBuffer(frameResource, spotLightConstants, light.LightViewProjection, light.LightPosition);
        }
        else
        {
            sceneConstantBuffer = GenerateSceneConstantBuffer(frameResource, spotLightConstants, vp, cameraPos);
        }

        frameResource.BindRenderTarget();
        
        //RenderMesh(frameResource, true);
        RenderMesh(frameResource, sceneConstantBufferKey, false);
    }

    private SceneConstantBuffer GenerateSceneConstantBuffer(FrameResource frameResource, LightConstant[] spotLightConstants, Matrix4x4 vp, Vector3 cameraPosition)
    {
        var set = frameResource.Settings.GetSettings<Settings>();

        var sceneConstantBuffer = new SceneConstantBuffer
        {
            viewTransform = vp,
            CameraPosition = cameraPosition,
            LightCount = spotLightConstants.Length,
            NormalScale = set.NormalScale
        };

        for (int i = 0; i < spotLightConstants.Length; i++)
            sceneConstantBuffer.lightConstants[i] = spotLightConstants[i];
        
        return sceneConstantBuffer;    
    }

    private void RenderMesh(FrameResource frameResource, ConstantBufferKey<SceneConstantBuffer> sceneConstantBufferKey, bool DepthPass = false)
    {

        var set = frameResource.Settings.GetSettings<Settings>();
        var cmd = frameResource.CommandList;

        foreach (var MaterialDefinitionGrouping in PrimitivesToRender)
        {
            MaterialDefinition MaterialDefinition = MaterialDefinitionGrouping.MaterialDefinition;

            //Skip depth pass if material is transparent
            if (MaterialDefinition.alphaMode == AlphaMode.BLEND && DepthPass)
                continue;

            if (MaterialDefinition.PermutationKey.TwoSided && set.showDoubleSided <= 0)
            {
                continue;
            }
            if ((!MaterialDefinition.PermutationKey.TwoSided) && set.showSingleSided <= 0)
            {
                continue;
            }
            if (DepthPass)
            {
                MaterialDefinition.BindDepthPass(frameResource);
            }
            else
            {
                MaterialDefinition.Bind(frameResource);
            }

            cmd.SetGraphicsRootConstantBufferView(MaterialDefinition.RootSignatureDefinition.SceneBind(), 
                    frameResource.GetGPUVirtualAddress(sceneConstantBufferKey));
            
            foreach (var nodeGrouping in MaterialDefinitionGrouping.Nodes)
            {
                Node node = nodeGrouping.Key;
                var mesh = node.Mesh!;

                var meshBuffer = new MeshConstantBuffer();
                Matrix4x4.Invert(node.GlobalMatrix, out meshBuffer.inverseModelTransform);

                meshBuffer.inverseModelTransform = Matrix4x4.Transpose(meshBuffer.inverseModelTransform);
                meshBuffer.modelTransform = node.GlobalMatrix;

                frameResource.GetBufferConstantRef(mesh.constantBufferKey) = meshBuffer;
                cmd.SetGraphicsRootConstantBufferView(MaterialDefinition.RootSignatureDefinition.MeshBind(), 
                        frameResource.GetGPUVirtualAddress(mesh.constantBufferKey));


                IEnumerable<Primitive> selected = mesh.Primitives;

                foreach (var primitive in nodeGrouping)
                {
                    if (primitive.Material == null || !primitive.Material.Visible)
                    {
                        continue;
                    }

                    primitive.Material.Bind(frameResource, MaterialDefinition.RootSignatureDefinition.MaterialBind());

                    node.Skin?.BindSkin(frameResource, primitive.MaterialDefinition.RootSignatureDefinition.SkeletalMeshBind());


                    cmd.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
                    cmd.IASetVertexBuffers(0, primitive.VertexBufferView);
                    cmd.IASetIndexBuffer(primitive.IndexBufferView);
                    cmd.DrawIndexedInstanced((uint)primitive.IndexCount, 1, 0, 0, 0);
                }
            }
        }
    }

    public record struct MaterialGrouping(MaterialDefinition MaterialDefinition, List<IGrouping<Node, Primitive>> Nodes)
    {
    }

    private void updateNodePositions(FrameResource frameResource)
    {
        foreach (var i in Nodes)
        {
            i.LocalTransform = i.DefaultTransform;
        }

        var set = frameResource.Settings.GetSettings<Settings>();

        foreach (var animation in Animations)
            if (animation != null)
            {
                animation.animationDelta += frameResource.FrameMetric.DeltaTime * set.AnimationSpeed;
                animation.animationDelta %= animation.TotalTime;

                foreach (var animationNode in animation.AnimationNodes)
                {
                    animationNode.UpdateTransform(animation.animationDelta);
                }
            }

        foreach (var i in RootNodes)
        {
            i.UpdateWorldTransforms(AffineTransform.Identity);
        }
    }


}

public class Camera
{
    public required Node Node;
    public float yfov;
    public float zfar;
    public float znear;
}