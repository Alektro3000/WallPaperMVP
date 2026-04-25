using System.Diagnostics;
using System.Numerics;

namespace TextSystem;

public class Controller
{
    private readonly ParticleBuffers ParticleSystem;


    public Controller(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateConstantBuffer(ref Constants constant, FrameMetric frameMetric, SystemSettings systemSettings)
    {
        // Update static buffer
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.Settings = systemSettings.textSettings;
    }
}