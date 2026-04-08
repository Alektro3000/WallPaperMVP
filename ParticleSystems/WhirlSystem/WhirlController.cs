using System.Diagnostics;
using System.Numerics;

public class WhirlController
{
    private readonly ParticleBuffers ParticleSystem;


    public WhirlController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref WhirlConstants constant, FrameMetric frameMetric)
    {
        // Update static buffer
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 3f;
        constant.SpawnRate = 150f;
        constant.CenterPosition = new Vector2(0f,0.2f);
    }
}