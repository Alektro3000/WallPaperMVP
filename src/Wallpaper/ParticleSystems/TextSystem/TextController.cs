using System.Diagnostics;
using System.Numerics;

public class TextController
{
    private readonly ParticleBuffers ParticleSystem;
    private SystemSettings systemSettings;


    public TextController(ParticleBuffers partcileSystem, SystemSettings settings)
    {
        systemSettings = settings;
        ParticleSystem = partcileSystem;
    }

    public void UpdateConstantBuffer(ref TextConstants constant, FrameMetric frameMetric)
    {
        // Update static buffer
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.Settings = systemSettings.textSettings;
    }
}