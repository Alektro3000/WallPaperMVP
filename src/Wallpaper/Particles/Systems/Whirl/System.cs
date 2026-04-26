
using Renderer.Core;
using Vortice.Direct3D12;
using Particles.Settings;
using Particles.Core;
using Renderer.FrameManagement;
using Renderer.Resources;

namespace Particles.Systems.Whirl;

[Shader("whirl\\compute.hlsl", "cs")]
[Shader("whirl\\precompute.hlsl", "cs")]
public class ParticleSystem : BaseParticleSystem, IParticleSystem<Settings>
{
    protected Controller ParticleSystemController;
    public ParticleSystem(
        ParticleSystemInitContext context, Settings settings)
    {
        ConstructRequiredFields(context, (uint)settings.initSettings.MaxParticleAmount, "WhirlSystem", "whirl/compute.hlsl", "whirl/precompute.hlsl");
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
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey, BufferFactory.CreateConstantBuffer<Constants>(device, "WhirlSystem_Constant"));
    }
}