
using Particles.Core;
using Particles.Settings;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Particles.Systems.Corner;

public class ParticleSystem : BaseParticleSystem, IParticleSystemFor<Settings>
{
    protected Controller ParticleSystemController;
    private readonly ConstantBufferKey<Constants> ConstantKey;
    public override IConstantBufferKey ConstantBufferKey => ConstantKey;
    public ParticleSystem(
        ParticleSystemInitContext context, Settings settings) :
        base(context, (uint)settings.initSettings.MaxParticleAmount, "Corner")
    {
        ConstantKey = context.Registry.Reserve<Constants>("CornerSystem_Constant");
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