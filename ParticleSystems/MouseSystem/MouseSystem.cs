
using Vortice.Direct3D12;

public class MouseSystem : ParticleSystem
{
    protected MouseController ParticleSystemController;
    public MouseSystem(
        ID3D12Device device, 
        ImmediateCommandList commandList, 
        GeometryBuffers GeometryBuffers, 
        HeapAllocator HeapAllocator, 
        FrameManager FrameManager)
    {
        ParticleBuffers = new ParticleBuffers(device, commandList, HeapAllocator, 2048 * 4);
        GraphicPass = new GraphicPass(device, ParticleBuffers, GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        ComputePass = new ComputePass(device, ParticleBuffers, "mouse/compute.hlsl", "mouse/precompute.hlsl");
        ParticleSystemController = new MouseController(ParticleBuffers);
        ConstantKey = FrameManager.ReserveBuffer();
    }
    public void UpdateMouseSettings(MouseSettings mouseSettings)
    {
        ParticleSystemController.mouseSettings = mouseSettings;
    }
    public override void UpdateStaticResource(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBuffer(ConstantKey).Constants<MouseConstants>(),
        currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {        
        unsafe
        {
            var constantBuffer = BufferHelper.CreateStaticBuffer(device, out MouseConstants* MappedConstants);
            
            var binding = new FrameResource.ConstantBinding
            {
                ConstantBuffer = constantBuffer,
                MappedConstants = (byte*)MappedConstants,
            };
            
            frameResource.AddBuffer(ConstantKey, binding);
        }
    }
}