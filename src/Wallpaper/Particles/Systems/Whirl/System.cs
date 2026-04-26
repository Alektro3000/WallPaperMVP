
using Renderer.Core;
using Vortice.Direct3D12;
using Particles.Settings;
using Particles.Core;
using Renderer.FrameManagement;
using Renderer.Resources;

namespace Particles.Systems.Whirl;

[Shader("whirl\\compute.hlsl", "cs")]
[Shader("whirl\\precompute.hlsl", "cs")]
public class ParticleSystem : BaseParticleSystem, IParticleSystemFor<Settings>
{
    protected Controller ParticleSystemController;
    public ParticleSystem(
        ParticleSystemInitContext context, Settings settings) : 
        base(context, (uint)settings.initSettings.MaxParticleAmount, "WhirlSystem", "whirl/compute.hlsl", "whirl/precompute.hlsl")
    {

        ConstantKey = context.Registry.Reserve(device => BufferFactory.CreateConstantBuffer<Constants>(device, "WhirlSystem_Constant"));
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