

using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct SceneConstantBuffer
{
    public Matrix4x4 viewTransform;
    public LightConstantBufferHelper lightConstants;
    public Vector3 CameraPosition;
    public int LightCount;
    public float NormalScale;
}