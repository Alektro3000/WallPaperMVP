
using Vortice.Direct3D12;

[Shader("mouse\\compute.hlsl", "cs")]
[Shader("mouse\\precompute.hlsl", "cs")]
public class MouseSystem : ParticleSystem
{
    protected MouseController ParticleSystemController;
    public MouseSystem(InitContext context)
    {
        ConstructRequiredFields(context, 4096, "mouse/compute.hlsl", "mouse/precompute.hlsl");
        ParticleSystemController = new MouseController(ParticleBuffers);
    }
    public void UpdateMouseSettings(MouseSettings mouseSettings)
    {
        ParticleSystemController.mouseSettings = mouseSettings;
    }
    public override void UpdateConstantBuffers(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<MouseConstants>(ConstantKey),
        currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<MouseConstants>(device));
    }
}