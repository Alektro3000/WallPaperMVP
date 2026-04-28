
using Particles.Core;
using Particles.Passes;
using Particles.Resources;
using Particles.Settings;
using Particles.Shared.Field;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Passes;
using Renderer.Resources;
using Vortice.Direct3D12;


namespace Particles.Systems.Mouse;

[Shader("mouse\\compute.hlsl", "cs")]
[Shader("mouse\\emitter.hlsl", "cs")]
[Shader("mouse\\draw_count.hlsl", "cs")]
public class ParticleSystem : BaseParticleSystem, IParticleSystemFor<Settings>
{
    protected Controller ParticleSystemController;
    public ParticleSystem(
        ParticleSystemInitContext context, Settings settings) :
        base(context, (uint)settings.initSettings.MaxParticleAmount, "Mouse")
    {
        ConstantKey = context.Registry.Reserve(device => BufferFactory.CreateConstantBuffer<Constants>(device, "MouseSystem_Constant"));
        ParticleSystemController = new Controller(ParticleBuffers);
    }

    [SystemBuilder]
    public static ParticleSystem? Create(ParticleSystemInitContext context, Settings settings)
    {
        if (settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new ParticleSystem(context, settings);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<Constants>(ConstantKey),
            currentResource.frameMetric, systemSettings);
    }
}
