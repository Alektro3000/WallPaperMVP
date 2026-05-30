
using System.Configuration;
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct LightConstant
{   
    public required Vector3 LightPosition;                  // xyz position
    public required float InvRadius;                        //w = 1/radius
    public required Vector3 LightColor;                     // rgb color/intensity
    public required float FalloffExponent;                  // controls the falloff curve
    public required Vector4 SpotAnglesAndSourceRadius;      // cone angles + source radius
    public required Vector3 LightDirection;                 // for spot / directional
    public required float SoftSourceRadius;
    public int ShadowDescriptionBegin;
    private Vector3 padding;
}