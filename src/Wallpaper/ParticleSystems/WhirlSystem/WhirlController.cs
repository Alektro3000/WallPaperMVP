using System.Diagnostics;
using System.Numerics;

public class WhirlController
{
    private readonly ParticleBuffers ParticleSystem;


    public WhirlController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref WhirlConstants constant, FrameMetric frameMetric, SystemSettings systemSettings)
    {
        // Update static buffer
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.WhirlSettings = systemSettings.whirlSettings;
    }
}