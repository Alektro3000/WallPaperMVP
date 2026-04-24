using System.Diagnostics;
using System.Numerics;

namespace CornerSystem;

public class Controller
{
    private readonly ParticleBuffers ParticleSystem;

    public Controller(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref Constants constant, FrameMetric frameMetric, SystemSettings systemSettings)
    {
        // Update static buffer
        float scale = frameMetric.width/(float)frameMetric.height;
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.settings = systemSettings.cornerSettings;
        constant.settings.SpawnPosition += new Vector2(scale,1);
        constant.settings.RemoveBox += constant.settings.SpawnPosition;
    }
}