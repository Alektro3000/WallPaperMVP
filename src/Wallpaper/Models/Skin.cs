
using System.Numerics;
using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Models;
public class Skin : IDisposable
{
    public Node[] JointMapping = [];
    public Matrix4x4[] InverseMatrixBind = [];

    private readonly ConstantBufferKey<JointsStaticBuffer> StaticJoints;
    public Skin(InitContext initContext, Node[] JointMapping, Matrix4x4[] InverseMatrix)
    {
        this.JointMapping = JointMapping;
        this.InverseMatrixBind = InverseMatrix;
        StaticJoints = initContext.ConstantBufferRegistry.Reserve<JointsStaticBuffer>("StaticJointsRegistry");
        
    }

    public void UpdateJointsPositions(FrameResource frameResource, Matrix4x4 RootTransform)
    {
        ref JointsStaticBuffer buffer = ref frameResource.GetBufferConstantRef(StaticJoints);
        Matrix4x4.Invert(RootTransform, out var rootInvert);
        for(int i = 0; i < JointMapping.Length; i++)
        {
            buffer.buffer[i] = InverseMatrixBind[i] * JointMapping[i].GlobalTransform * rootInvert;
        }
    }

    public void BindSkin(FrameResource frameResource)
    {
        frameResource.CommandList.SetGraphicsRootConstantBufferView(4, frameResource.GetGPUVirtualAddress(StaticJoints));
    }

    public void Dispose()
    {
    }
}