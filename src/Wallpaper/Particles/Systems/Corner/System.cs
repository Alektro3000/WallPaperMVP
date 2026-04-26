
using Particles.Core;
using Particles.Settings;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Particles.Systems.Corner;

[Shader("corner\\compute.hlsl", "cs")]
[Shader("corner\\precompute.hlsl", "cs")]
public class ParticleSystem : BaseParticleSystem, IParticleSystem<Settings>
{
    protected Controller ParticleSystemController;
    public ParticleSystem(
        ParticleSystemInitContext context, Settings settings) :
        base(context, (uint)settings.initSettings.MaxParticleAmount, "CornerSystem", "corner/compute.hlsl", "corner/precompute.hlsl")
    {
        ConstantKey = context.Registry.Reserve(device => BufferFactory.CreateConstantBuffer<Constants>(device, "CornerSystem_Constant"));
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