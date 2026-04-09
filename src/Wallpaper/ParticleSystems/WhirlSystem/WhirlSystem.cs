
using Vortice.Direct3D12;

[Shader("whirl\\compute.hlsl", "cs")]
[Shader("whirl\\precompute.hlsl", "cs")]
public class WhirlSystem : ParticleSystem
{
    protected WhirlController ParticleSystemController;
    public WhirlSystem(
        InitContext context)
    {
        ConstructRequiredFields(context, 2048, "whirl/compute.hlsl", "whirl/precompute.hlsl");
        ParticleSystemController = new WhirlController(ParticleBuffers);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<WhirlConstants>(ConstantKey),
            currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<WhirlConstants>(device));
    }
}