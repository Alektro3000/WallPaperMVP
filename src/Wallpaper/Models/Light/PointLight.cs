using System.Numerics;
using Renderer.FrameManagement;
using Renderer.Resources;

namespace Models.Lights;
public class PointLight : PrincipledLight
{
    public float Intensity;
    public Vector3 Color;
    public required Node Node;
    public float Radius;
    public float? SourceRadius;
    public float? SoftSourceRadius;

    public override void BindRenderTarget(FrameResource frameResource, int shadowDescriptorIndex)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        
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
            // w   = falloff exponent
            LightColor = Color * Intensity,

            FalloffExponent = 2.0f, // TODO: make this configurable?

            // x = cos(outer cone)
            // y = inverse cone difference
            // z = unused/helper
            // w = source radius
            SpotAnglesAndSourceRadius = new Vector4(
                0.0f,
                0.0f,
                1.0f,
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
        };
    }

    public override Matrix4x4 GetLightViewProjection()
    {
        throw new NotImplementedException();
    }

    public override ConstantBufferKey<SceneConstantBuffer> GetSceneConstantKey(int shadowDescriptorIndex)
    {
        throw new NotImplementedException();
    }

    public override int GetShadowDescriptorCount() => 6;

    public override void UnbindRenderTarget(FrameResource frameResource, int shadowDescriptorIndex)
    {
        throw new NotImplementedException();
    }
}