
using Vortice.Direct3D12;

namespace WhirlSystem;

[Shader("whirl\\compute.hlsl", "cs")]
[Shader("whirl\\precompute.hlsl", "cs")]
public class WhirlSystem : ParticleSystem
{
    protected Controller ParticleSystemController;
    public WhirlSystem(
        InitContext context)
    {
        ConstructRequiredFields(context, 2048, "WhirlSystem", "whirl/compute.hlsl", "whirl/precompute.hlsl");
        ParticleSystemController = new Controller(ParticleBuffers);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<Constants>(ConstantKey),
            currentResource.frameMetric, systemSettings);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<Constants>(device, "WhirlSystem_Constant"));
    }
}