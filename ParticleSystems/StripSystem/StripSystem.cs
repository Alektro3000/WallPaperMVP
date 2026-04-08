
using Vortice.Direct3D12;

public class StripSystem : ParticleSystem
{
    protected StripController ParticleSystemController;
    public StripSystem(InitContext context)
    {
        ConstructRequiredFields(context, 2048 * 2, "strip/compute.hlsl", "strip/precompute.hlsl");
        ParticleSystemController = new StripController(ParticleBuffers);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<StripConstants>(ConstantKey),
            currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<StripConstants>(device));
    }
}