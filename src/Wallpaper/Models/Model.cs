using System.Numerics;
using System.Runtime.InteropServices;
using Renderer.FrameManagement;
using Renderer.Resources;
using Settings;
using SharpGen.Runtime;
using Vortice.Direct3D12;

namespace Models;


public class Settings : ISettings
{
    public Vector3 cameraPos;
    public Vector3 centerPos;

    public float ShowComponent;
    public float CameraSpeed;
    public float showUV;
    public float showNormal;
}


public sealed class Model : IDisposable
{
    public List<Material> Materials = [];
    public required MeshBuffer MeshBuffer;
    public List<Skin> Skins = [];
    public List<Mesh> Meshes = [];
    public List<Node> Nodes = [];
    public List<Node> RootNodes = [];
    public required TextureProvider textureProvider;
    
    public Model(InitContext initContext)
    {
        
    }
    public void Dispose(){
        foreach(var skin in Skins)
        {
            skin.Dispose();
        }
        foreach(var mesh in Meshes)
        {
            mesh.Dispose();
        }
        foreach(var material in Materials)
        {
            material.Dispose();
        }
        MeshBuffer.Dispose();
    }

    public void Render(FrameResource frameResource)
    {
        foreach(var i in RootNodes)
        {
            i.UpdateWorldTransforms(Matrix4x4.Identity);
        }

        foreach(var i in Nodes.Where(x=>x.Skin != null))
        {
            i.Skin?.UpdateJointsPositions(frameResource, i.GlobalTransform);
        }


        RenderMesh(frameResource);
    }

    public void RenderMesh(FrameResource frameResource)
    {
        
        var set = frameResource.Settings.GetSettings<Settings>();
        
        float time = frameResource.FrameMetric.FrameIndex * set.CameraSpeed;
        var viewMatrix = Matrix4x4.CreateLookAt(
                cameraPosition: set.centerPos+ new Vector3((float)(set.cameraPos.X * Math.Sin(time)), set.cameraPos.Y, (float)(set.cameraPos.X * Math.Cos(time))),
                cameraTarget: set.centerPos,
                cameraUpVector: Vector3.UnitY);
                
        var projection =
            Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfView: MathF.PI / 4.0f,
                aspectRatio: frameResource.FrameMetric.ratio,
                nearPlaneDistance: 0.1f,
                farPlaneDistance: 100.0f);

        foreach(var node in Nodes.Where(x=>x.Mesh != null))
        {
            var cmd = frameResource.CommandList;
            var mesh = node.Mesh!;
            
            var mvp = viewMatrix * projection;
            frameResource.GetBufferConstantRef(mesh.constantBufferKey) =  mvp;

            IEnumerable<Primitive> selected = mesh.Primitives;

            if(set.ShowComponent >= 0)
                selected = selected.Skip((int)set.ShowComponent).Take(1);

            foreach(var primitive in selected)
            {
                if(primitive.Material == null)
                {
                    continue;
                }
                primitive.Material.BindMaterial(frameResource);
                node.Skin?.BindSkin(frameResource);
                cmd.SetGraphicsRootConstantBufferView(0, frameResource.GetGPUVirtualAddress(mesh.constantBufferKey));

                cmd.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
                cmd.IASetVertexBuffers(0, primitive.VertexBufferView);
                cmd.IASetIndexBuffer(primitive.IndexBufferView);
                cmd.DrawIndexedInstanced((uint)primitive.IndexCount, 1, 0, 0, 0);
            }
        }
    }


}
