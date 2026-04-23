
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct DispatchArgs
{
    public uint ThreadGroupCountX;
    public uint ThreadGroupCountY;
    public uint ThreadGroupCountZ;
}