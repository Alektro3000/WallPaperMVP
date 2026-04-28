
using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Settings;

namespace Particles.Systems.Text;


[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public uint ParticleCount;
    public Vector3 padding;
    public GpuSettings Settings;
}
