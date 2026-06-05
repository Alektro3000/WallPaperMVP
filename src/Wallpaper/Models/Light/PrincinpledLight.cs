
using System.Numerics;
using Renderer.FrameManagement;
using Renderer.Resources;

public abstract class PrincipledLight : IDisposable
{
    public abstract LightConstant GetLightConstant();
    public abstract int GetShadowDescriptorCount();
    public abstract Matrix4x4 GetLightViewProjection();
    public abstract void BindRenderTarget(FrameResource frameResource, int shadowDescriptorIndex);

    public abstract void Dispose();

    public abstract void UnbindRenderTarget(FrameResource frameResource, int shadowDescriptorIndex);
    public abstract ConstantBufferKey<SceneConstantBuffer> GetSceneConstantKey(int shadowDescriptorIndex);
}