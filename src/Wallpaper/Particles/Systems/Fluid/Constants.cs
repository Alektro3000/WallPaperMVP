using System.Numerics;
using System.Runtime.InteropServices;

namespace Particles.Systems.Fluid;

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public uint ParticleCount;
    public uint RangeCount;
    public Vector2 MousePos;

    public GpuSettings Settings;
}
