using System.Diagnostics;
using System.Numerics;

public class WhirlController
{
    private readonly ParticleBuffers ParticleSystem;

    private SystemSettings systemSettings;

    public WhirlController(ParticleBuffers partcileSystem, SystemSettings settings)
    {
        systemSettings = settings;
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref WhirlConstants constant, FrameMetric frameMetric)
    {
        // Update static buffer
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.WhirlSettings = systemSettings.whirlSettings;
    }
}