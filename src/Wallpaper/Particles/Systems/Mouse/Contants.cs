using System.Numerics;
using System.Runtime.InteropServices;

namespace Particles.Systems.Mouse;

[StructLayout(LayoutKind.Sequential)]
public struct CpuGeneratedConstants
{
    public Vector2 CatmulA;
    public Vector2 CatmulB;
    public Vector2 CatmulC;
    public Vector2 CatmulD;

    public uint ParticleCount;
    public float VelocityBlend;
    public Vector2 MousePos;

    public float DistanceP1P2;
    public float PhaseShift;
    public float WavePhaseOnSegment ;
    public float MouseSpeed;
    
    public float Size;
    public Vector2 GridSize;
    private uint _padding = 0;

    public CpuGeneratedConstants()
    {
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Constants
{
    public CpuGeneratedConstants cpuGeneratedSettings = new CpuGeneratedConstants();
    public GpuSettings Settings = new GpuSettings();

    public Constants()
    {
    }
}