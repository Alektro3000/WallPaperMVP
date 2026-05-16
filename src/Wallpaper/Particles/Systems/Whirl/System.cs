
using Renderer.Core;
using Vortice.Direct3D12;
using Particles.Settings;
using Particles.Core;
using Renderer.FrameManagement;
using Renderer.Resources;

namespace Particles.Systems.Whirl;

public class ParticleSystem : BaseParticleSystem, IParticleSystemFor<Settings>
{
    protected Controller ParticleSystemController;

    private readonly ConstantBufferKey<Constants> ConstantKey;
    public override IConstantBufferKey ConstantBufferKey => ConstantKey;
    public ParticleSystem(
        ParticleSystemInitContext context, Settings settings) : 
        base(context, (uint)settings.initSettings.MaxParticleAmount, "Whirl")
    {

        ConstantKey = context.Registry.Reserve<Constants>("WhirlSystem_Constant");
        ParticleSystemController = new Controller(ParticleBuffers);
    }

    [SystemBuilder]
    public static ParticleSystem? Create(ParticleSystemInitContext context, Settings settings)
    {
        if (settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new ParticleSystem(context, settings);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<Constants>(ConstantKey),
            currentResource.FrameMetric, currentResource.Settings);
    }
}