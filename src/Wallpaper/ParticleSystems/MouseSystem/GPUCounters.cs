using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct GpuMouseBuffer
{
    public float VelocityBlend;
    public uint Pad0;
    public uint Pad1;
    public uint Pad2;
}

[StructLayout(LayoutKind.Sequential)]
public struct DispatchArgs
{
    public uint ThreadGroupCountX;
    public uint ThreadGroupCountY;
    public uint ThreadGroupCountZ;
}