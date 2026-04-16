using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct CommonConstantBuffer
{
    public Matrix4x4 viewMatrix;
    public float DeltaTime;
    public uint FrameIndex;
    public uint width;
    public uint height;
    public float ScreenRatio;
    public float ScreenRatioInv;
}