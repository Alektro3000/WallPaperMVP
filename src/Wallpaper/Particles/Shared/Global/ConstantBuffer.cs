using System.Numerics;
using System.Runtime.InteropServices;


namespace Particles.Shared.Global;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBuffer
{
    public Matrix4x4 viewMatrix;
    public float DeltaTime;
    public uint FrameIndex;
    public uint width;
    public uint height;
    public float ScreenRatio;
    public float ScreenRatioInv;
}