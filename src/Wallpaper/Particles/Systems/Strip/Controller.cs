using Particles.Resources;
using Particles.Settings;
using Renderer.FrameManagement;
using Settings;

namespace Particles.Systems.Strip;
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
        var cpu = systemSettings.GetSettings<Settings>().cpuSettings;
        constant.strip0 = CreateStrip(0f, 0f, 0, cpu);
        constant.strip1 = CreateStrip(1f, -1f, 1f, cpu);
        constant.strip2 = CreateStrip(2f, +1f, 2f, cpu);
        constant.strip3 = CreateStrip(3f, -2f, 3f, cpu);
        constant.strip4 = CreateStrip(4f, +2f, 4f, cpu);


        constant.Size = cpu.Size / frameMetric.height;
        constant.GridSize = cpu.GridSize / frameMetric.height;
    }

    public Constants.StripDescription CreateStrip(float x, float y, float ys, CpuSettings cpu)
    {
        return new Constants.StripDescription(x*cpu.StripDistance+cpu.StripOffset,y * cpu.StripHeightOffset,0.05f,ys * cpu.StripHeightScaling + cpu.StripHeightBase);
    }
}