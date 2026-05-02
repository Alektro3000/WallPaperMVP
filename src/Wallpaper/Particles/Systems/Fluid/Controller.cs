using System.Numerics;
using Particles.Resources;
using Particles.Settings;
using Renderer.FrameManagement;

namespace Particles.Systems.Fluid;

public sealed class Controller
{
    private readonly ParticleBuffers particles;

    public Controller(ParticleBuffers particles)
    {
        this.particles = particles;
    }

    public void UpdateStaticResource(ref Constants constant, FrameMetric frameMetric, SystemSettings systemSettings)
    {
        var settings = systemSettings.GetSettings<Settings>().gpuSettings;

        Win32.GetCursorPos(out Win32.POINT point);
        var mouse = new Vector2(((float)point.X) / frameMetric.height, (frameMetric.height - (float)point.Y) / frameMetric.height) * 2 - new Vector2(1/frameMetric.ratio, 1);

        constant.ParticleCount = particles.particleCount;
        constant.RangeCount = FluidCompute.MaxGridCells;
        constant.MousePos = mouse;
        constant.Settings = settings;
    }
}
