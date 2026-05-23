using System.Numerics;
using System.Runtime.InteropServices;
using Models.Material;
using Renderer.FrameManagement;
using Renderer.Resources;
using Settings;
using SharpGen.Runtime;
using Vortice.Direct3D12;

namespace Models;


public class Settings : ISettings
{
    [UiRange(0,1,1)]
    public float useFreeCamera;
    public Vector3 cameraPos;
    public Vector3 centerPos;

    public float CameraSpeed;
    [UiRange(0,1,1)]
    public float showUV;
    [UiRange(0,1,1)]
    public float showNormal;
}


public sealed class Model : IDisposable
{
    public AffineTransform CameraTransform;
    public List<MaterialInstance> Materials = [];
    public List<MaterialDefinition> MaterialDefinitions = [];
    public List<RootSignatureDefinition> RootSignatureDefinitions = [];
    public required MeshBuffer MeshBuffer;
    public List<Skin> Skins = [];
    public List<Mesh> Meshes = [];
    public List<Node> Nodes = [];
    public List<Node> RootNodes = [];
    public List<Animation> Animations = [];
    public required TextureLoader TextureLoader;
    public float rotationDelta = 0;
    public Model(InitContext initContext)
    {

    }
    public void Dispose()
    {
        foreach (var skin in Skins)
        {
            skin.Dispose();
        }
        foreach (var mesh in Meshes)
        {
            mesh.Dispose();
        }
        TextureLoader.Dispose();
        foreach (var materialDefinition in MaterialDefinitions)
        {
            materialDefinition.Dispose();
        }
        foreach (var rootSignatureDefinition in RootSignatureDefinitions)
        {
            rootSignatureDefinition.Dispose();
        }
        MeshBuffer.Dispose();
    }

    public void Render(FrameResource frameResource)
    {
        foreach (var i in Nodes)
        {
            i.LocalTransform = i.DefaultTransform;
        }

        foreach(var animation in Animations)
            if(animation != null)
            {
                animation.animationDelta += frameResource.FrameMetric.DeltaTime;
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

        foreach (var i in Nodes.Where(x => x.Skin != null))
        {
            i.Skin?.UpdateJointsPositions(frameResource, i.GlobalMatrix);
        }

        RenderMesh(frameResource);
    }

    public void RenderMesh(FrameResource frameResource)
    {

        var set = frameResource.Settings.GetSettings<Settings>();

        rotationDelta += frameResource.FrameMetric.DeltaTime * set.CameraSpeed;
        rotationDelta %= (float)(Math.PI * 2);

        var rotation = Quaternion.CreateFromAxisAngle(new Vector3(0,1,0), rotationDelta);


        var viewMatrix = Matrix4x4.Invert(CameraTransform.Matrix, out var inv)
            ? inv
            : Matrix4x4.Identity;
        
        if(set.useFreeCamera > 0)
            viewMatrix = Matrix4x4.CreateLookAt(
                cameraPosition: set.centerPos + Vector3.Transform(set.cameraPos, rotation),
                cameraTarget: set.centerPos,
                cameraUpVector: Vector3.UnitY);

        var projection =
            Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfView: MathF.PI / 4.0f,
                aspectRatio: frameResource.FrameMetric.ratio,
                nearPlaneDistance: 0.1f,
                farPlaneDistance: 100.0f);

        var PrimitivesToRender = 
            Nodes
            .Where(x => x.Mesh != null)
            .SelectMany(node => node.Mesh.Primitives.Select(p => (node, p)).ToList<(Node node, Primitive primitive)>() )
            .GroupBy(x=>x.primitive.MaterialDefinition)
            .Select(materialGroup => new
            {
                MaterialDefinition = materialGroup.Key,
                Nodes = materialGroup
                    .GroupBy(x => x.node)
            })
            .ToList();

        foreach (var MaterialDefinitionGrouping in PrimitivesToRender)
        {
            MaterialDefinition MaterialDefinition = MaterialDefinitionGrouping.MaterialDefinition;
            var cmd = frameResource.CommandList;
            MaterialDefinition.Bind(frameResource);
            
            foreach (var nodeGrouping in MaterialDefinitionGrouping.Nodes)
            {
                var node = nodeGrouping.Key;
                var mesh = node.Mesh!;

                var mvp = node.GlobalMatrix  * viewMatrix * projection;
                frameResource.GetBufferConstantRef(mesh.constantBufferKey) = mvp;

                IEnumerable<Primitive> selected = mesh.Primitives;
                
                foreach (var primitivePair in nodeGrouping)
                {
                    var primitive = primitivePair.primitive;
                    
                    if (primitive.Material == null)
                    {
                        continue;
                    }

                    primitive.Material.Bind(frameResource);
                    
                    node.Skin?.BindSkin(frameResource, primitive.MaterialDefinition.RootSignatureDefinition.SkeletalMeshBind());
                        
                    cmd.SetGraphicsRootConstantBufferView(0, frameResource.GetGPUVirtualAddress(mesh.constantBufferKey));

                    cmd.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
                    cmd.IASetVertexBuffers(0, primitive.VertexBufferView);
                    cmd.IASetIndexBuffer(primitive.IndexBufferView);
                    cmd.DrawIndexedInstanced((uint)primitive.IndexCount, 1, 0, 0, 0);
                }
            }
        }        
    }


}
