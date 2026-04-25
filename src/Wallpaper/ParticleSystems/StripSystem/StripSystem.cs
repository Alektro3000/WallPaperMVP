
using Vortice.Direct3D12;

namespace ParticleSystems.Strip;

[Shader("strip\\compute.hlsl", "cs")]
[Shader("strip\\precompute.hlsl", "cs")]
public class System : ParticleSystem, IParticleSystem<Settings>
{
    protected Controller ParticleSystemController;
    public System(InitContext context, Settings settings)
    {
        ConstructRequiredFields(context, (uint)settings.initSettings.MaxParticleAmount, "StripSystem", "strip/compute.hlsl", "strip/precompute.hlsl");
        ParticleSystemController = new Controller(ParticleBuffers);
    }
    
    [SystemBuilder]
    public static System? Create(InitContext context, Settings settings)
    {
        if(settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new System(context, settings);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<Constants>(ConstantKey),
            currentResource.frameMetric, systemSettings);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<Constants>(device, "StripSystem_Constant"));
    }
}