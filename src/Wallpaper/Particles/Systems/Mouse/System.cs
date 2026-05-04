
using Particles.Core;
using Particles.Settings;
using Renderer.FrameManagement;
using Renderer.Resources;


namespace Particles.Systems.Mouse;

[Shader("mouse\\compute.hlsl", "cs")]
[Shader("mouse\\emitter.hlsl", "cs")]
[Shader("mouse\\draw_count.hlsl", "cs")]
public class ParticleSystem : BaseParticleSystem, IParticleSystemFor<Settings>
{
    protected Controller ParticleSystemController;
    protected ConstantBufferKey<Constants> ConstantKey;
    public override IConstantBufferKey ConstantBufferKey => ConstantKey;
    public ParticleSystem(
        ParticleSystemInitContext context, Settings settings) :
        base(context, (uint)settings.initSettings.MaxParticleAmount, "Mouse")
    {
        ConstantKey = context.Registry.Reserve<Constants>("MouseSystem_Constant");
        ParticleSystemController = new Controller(ParticleBuffers);
    }


    [SystemBuilder]
    public static ParticleSystem? Create(ParticleSystemInitContext context, Settings settings)
    {
        if (settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new ParticleSystem(context, settings);
    }
    public override void Dispatch(FrameResource currentResource)
        => ComputePass.DispatchParticles(currentResource, ConstantBufferKey, true);

    public override void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef(ConstantKey),
            currentResource.frameMetric, systemSettings);
    }
}
