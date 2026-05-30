
using System.Numerics;

namespace Models.Lights;

public class SpotLight : PrincipledLight
{
    public float Intensity;
    public Vector3 Color;
    public float Radius;
    public float? SourceRadius;
    public float? SoftSourceRadius;
    public Node Node;
    public float InnerConeAngle;
    public float OuterConeAngle;

    public override LightConstant GetLightConstant()
    {
        return new LightConstant()
        {
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
        };
    }

    public override int GetShadowDescriptorCount() => 1;
}