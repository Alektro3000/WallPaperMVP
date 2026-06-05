
using System.Numerics;
using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Models.Lights;

public class SpotLight(InitContext initContext, BindlessTextureProvider textureProvider) : PrincipledLight
{
    const int shadowMapSize = 1024;

    public float Intensity;
    public Vector3 Color;
    public float Radius;
    public float? SourceRadius;
    public float? SoftSourceRadius;
    public Node Node;
    public float InnerConeAngle;
    public float OuterConeAngle;

    private readonly Viewport viewport = new (shadowMapSize, shadowMapSize);
    private readonly RectI scissor = new (shadowMapSize, shadowMapSize);
    public DepthRenderTarget ShadowRenderTarget = new (initContext.GraphicsContext, shadowMapSize, shadowMapSize, true);
    public BindlessTextureProvider TextureProvider { get; } = textureProvider;
    public ConstantBufferKey<SceneConstantBuffer> SceneConstantKey = initContext.ConstantBufferRegistry.Reserve<SceneConstantBuffer>("Light Scene Constant Buffer");

    public override void BindRenderTarget(FrameResource frameResource, int shadowDescriptorIndex)
    {
        var cmd = frameResource.CommandList;
        cmd.ResourceBarrierTransition(
            ShadowRenderTarget.TextureResource,
            ResourceStates.PixelShaderResource,
            ResourceStates.DepthWrite);

        cmd.ClearDepthStencilView(
            ShadowRenderTarget.Descriptor.Cpu,
            ClearFlags.Depth,
            0.0f,
            0);

        cmd.RSSetScissorRect(scissor);
        cmd.RSSetViewport(viewport);

        cmd.OMSetRenderTargets([], ShadowRenderTarget.Descriptor.Cpu);
    }
    public override void UnbindRenderTarget(FrameResource frameResource, int shadowDescriptorIndex)
    {
        frameResource.CommandList.ResourceBarrierTransition(
            ShadowRenderTarget.TextureResource,
            ResourceStates.DepthWrite,
            ResourceStates.PixelShaderResource);
    }

    public override void Dispose()
    {
        ShadowRenderTarget.Dispose();
    }

    public override LightConstant GetLightConstant()
    {
        return new LightConstant()
        {
            LightViewProjection = GetLightViewProjection(),
            // xyz = world position
            LightPosition = Node.GlobalTransform.Translation,
            
            // inverse light influence radius
            InvRadius = 1.0f / Radius,

            // rgb = light color * intensity
            LightColor = Color * Intensity * 0.01f,
            
            
            FalloffExponent = 2.0f, // TODO: make this configurable?

            // x = cos(outer cone)
            // y = inverse cone difference
            // z = unused/helper
            // w = source radius
            SpotAnglesAndSourceRadius = new Vector4(
                MathF.Cos(OuterConeAngle),
                1.0f / MathF.Max(
                    MathF.Cos(InnerConeAngle) - MathF.Cos(OuterConeAngle),
                    0.001f
                ),
                0.0f,
                SourceRadius ?? 0.0f
            ),

            // normalized forward direction
            LightDirection = Vector3.Normalize(
                Vector3.Transform(
                    -Vector3.UnitZ,
                    Node.GlobalTransform.Rotation
                )
            ),

            // screen-space contact shadow distance
            SoftSourceRadius = SoftSourceRadius ?? 0.0f,
            
            ShadowDescriptionBegin = TextureProvider.GetOrCreateBindlessIndex(ShadowRenderTarget)
        };
    }

    public override Matrix4x4 GetLightViewProjection()
    {
        var view = Matrix4x4.CreateLookAt(
            Node.GlobalTransform.Translation,
            Node.GlobalTransform.Translation + 
                Vector3.Transform(
                    -Vector3.UnitZ,
                    Node.GlobalTransform.Rotation
                ),
            Vector3.UnitY
        );

        var projection = Matrix4x4Ex.CreatePerspectiveFieldOfViewReversedZ(
            OuterConeAngle * 2.0f,
            1.0f,
            0.2f,
            Radius
        );

        return  view * projection;
    }

    public override int GetShadowDescriptorCount() => 1;

    public override ConstantBufferKey<SceneConstantBuffer> GetSceneConstantKey(int shadowDescriptorIndex)
    {
        return SceneConstantKey;
    }
}
