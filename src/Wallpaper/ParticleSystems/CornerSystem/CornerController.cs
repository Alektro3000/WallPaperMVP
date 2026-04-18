using System.Diagnostics;
using System.Numerics;

public class CornerController
{
    private readonly ParticleBuffers ParticleSystem;

    public CornerController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref CornerConstants constant, FrameMetric frameMetric, SystemSettings systemSettings)
    {
        // Update static buffer
        float scale = frameMetric.width/(float)frameMetric.height;
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.settings = systemSettings.cornerSettings;
        constant.settings.SpawnPosition += new Vector2(scale,1);
        constant.settings.RemoveBox += constant.settings.SpawnPosition;
    }
}