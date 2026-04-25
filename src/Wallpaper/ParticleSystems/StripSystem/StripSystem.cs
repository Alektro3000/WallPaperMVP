
using Vortice.Direct3D12;

namespace StripSystem;

[Shader("strip\\compute.hlsl", "cs")]
[Shader("strip\\precompute.hlsl", "cs")]
public class StripSystem : ParticleSystem
{
    protected Controller ParticleSystemController;
    public StripSystem(InitContext context)
    {
        ConstructRequiredFields(context, 2048 * 2, "strip/compute.hlsl", "strip/precompute.hlsl");
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