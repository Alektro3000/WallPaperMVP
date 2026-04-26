using System.Diagnostics;
using System.Numerics;
using Particles.Core;
using Particles.Resources;
using Particles.Settings;
using Renderer.Core;
using Renderer.FrameManagement;

namespace Particles.Systems.Text;

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
        constant.Settings = systemSettings.GetSettings<Settings>().gpuSettings;
    }
}