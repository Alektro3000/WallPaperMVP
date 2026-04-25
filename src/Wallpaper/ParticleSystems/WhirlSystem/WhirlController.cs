using System.Diagnostics;
using System.Numerics;

namespace ParticleSystems.Whirl;

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
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.Settings = systemSettings.GetSettings<Settings>().gpuSettings;
    }
}