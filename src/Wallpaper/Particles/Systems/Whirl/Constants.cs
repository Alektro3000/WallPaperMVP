
using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;


namespace Particles.Systems.Whirl;

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public uint ParticleCount;
    public Vector3 _padding;
    public GpuSettings Settings = new GpuSettings();

    public Constants()
    {
    }
}
