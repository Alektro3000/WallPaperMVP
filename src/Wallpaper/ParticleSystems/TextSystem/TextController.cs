using System.Diagnostics;
using System.Numerics;

public class TextController
{
    private readonly ParticleBuffers ParticleSystem;


    public TextController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateConstantBuffer(ref TextConstants constant, FrameMetric frameMetric, SystemSettings systemSettings)
    {
        // Update static buffer
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.Settings = systemSettings.textSettings;
    }
}