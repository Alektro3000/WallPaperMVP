
using Vortice.Direct3D12;

namespace CornerSystem;

[Shader("corner\\compute.hlsl", "cs")]
[Shader("corner\\precompute.hlsl", "cs")]
public class CornerSystem : ParticleSystem
{
    protected Controller ParticleSystemController;
    public CornerSystem(
        InitContext context)
    {
        ConstructRequiredFields(context, 1024, "corner/compute.hlsl", "corner/precompute.hlsl");
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
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<Constants>(device));
    }
}