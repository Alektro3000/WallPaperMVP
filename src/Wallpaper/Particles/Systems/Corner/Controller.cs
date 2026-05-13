using System.Diagnostics;
using System.Numerics;
using Particles.Core;
using Particles.Resources;
using Particles.Settings;
using Renderer.Core;
using Renderer.FrameManagement;
using Settings;

namespace Particles.Systems.Corner;

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
        constant.Settings = systemSettings.GetSettings<Settings>().gpuSettings;
        constant.Settings.SpawnPosition += new Vector2(scale,1);
        constant.Settings.RemoveBox += constant.Settings.SpawnPosition;
    }
}