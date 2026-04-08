
using Vortice.Direct3D12;

public class CornerSystem : ParticleSystem
{
    protected CornerController ParticleSystemController;
    public CornerSystem(
        InitContext context)
    {
        ConstructRequiredFields(context, 1024, "corner/compute.hlsl", "corner/precompute.hlsl");
        ParticleSystemController = new CornerController(ParticleBuffers);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<CornerConstants>(ConstantKey),
            currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<CornerConstants>(device));
    }
}