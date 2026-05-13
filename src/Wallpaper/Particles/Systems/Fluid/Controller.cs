using System.Numerics;
using Particles.Resources;
using Particles.Settings;
using Renderer.FrameManagement;
using Settings;

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
        var settings = systemSettings.GetSettings<Settings>();
        var gpuSettings = settings.gpuSettings;

        Win32.GetCursorPos(out Win32.POINT point);
        var mouse = new Vector2(((float)point.X) / frameMetric.height, (frameMetric.height - (float)point.Y) / frameMetric.height) * 2 - new Vector2(frameMetric.ratio, 1);
        uint mouseButtons = 0;
        if ((Win32.GetAsyncKeyState(Win32.VK_LBUTTON) & 0x8000) != 0)
            mouseButtons |= 1u;
        if ((Win32.GetAsyncKeyState(Win32.VK_RBUTTON) & 0x8000) != 0)
            mouseButtons |= 2u;

        constant.ParticleCount = particles.particleCount;
        constant.RangeCount = FluidCompute.MaxGridCells;
        constant.MousePos = mouse;
        constant.MouseButtons = mouseButtons;
        constant.SubdividedTime = frameMetric.DeltaTime * settings.TimeScale / settings.Subdivides;
        constant.Settings = gpuSettings;
    }
}
