using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct MouseConstants
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
    public float WaveCyclesOnSegment;
    public float MouseSpeed;


    public MouseSettings mouseSettings;

}