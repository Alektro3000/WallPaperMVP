
using Particles.Core;
using Particles.Settings;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Particles.Systems.Strip;

[Shader("strip\\compute.hlsl", "cs")]
[Shader("strip\\precompute.hlsl", "cs")]
public class ParticleSystem : BaseParticleSystem, IParticleSystem<Settings>
{
    protected Controller ParticleSystemController;
    public ParticleSystem(ParticleSystemInitContext context, Settings settings)
    {
        ConstantKey = context.Registry.Reserve(device => BufferFactory.CreateConstantBuffer<Constants>(device, "StripSystem_Constant"));
        ConstructRequiredFields(context, (uint)settings.initSettings.MaxParticleAmount, "StripSystem", "strip/compute.hlsl", "strip/precompute.hlsl");
        ParticleSystemController = new Controller(ParticleBuffers);
    }
    
    [SystemBuilder]
    public static ParticleSystem? Create(ParticleSystemInitContext context, Settings settings)
    {
        if(settings.initSettings.MaxParticleAmount <= 0)
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