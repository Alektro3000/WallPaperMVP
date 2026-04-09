using System.Diagnostics;
using System.Numerics;

public class TextController
{
    private readonly ParticleBuffers ParticleSystem;


    public TextController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateConstantBuffer(ref TextConstants constant, FrameMetric frameMetric)
    {
        // Update static buffer
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 3f;
        constant.SpawnRate = 1000f;
    }
}