using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;

namespace Particles.Systems.Corner;

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public uint ParticleCount;
    private Vector3 _padding;
    public GpuSettings Settings;
}
