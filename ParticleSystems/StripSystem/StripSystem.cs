
using Vortice.Direct3D12;

public class StripSystem : ParticleSystem
{
    protected StripController ParticleSystemController;
    public StripSystem(
        ID3D12Device device, 
        ImmediateCommandList commandList, 
        GeometryBuffers GeometryBuffers, 
        HeapAllocator HeapAllocator, 
        FrameManager FrameManager)
    {
        ParticleBuffers = new ParticleBuffers(device, commandList, HeapAllocator, 2048);
        GraphicPass = new GraphicPass(device, ParticleBuffers, GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        ComputePass = new ComputePass(device, ParticleBuffers, "strip/compute.hlsl", "strip/precompute.hlsl");
        ParticleSystemController = new StripController(ParticleBuffers);
        ConstantKey = FrameManager.ReserveBuffer();
    }

    public override void UpdateStaticResource(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBuffer(ConstantKey).Constants<StripConstants>(),
            currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {        
        unsafe
        {
            var constantBuffer = BufferHelper.CreateStaticBuffer(device, out StripConstants* MappedConstants);
            
            var binding = new FrameResource.ConstantBinding
            {
                ConstantBuffer = constantBuffer,
                MappedConstants = (byte*)MappedConstants,
            };
            
            frameResource.AddBuffer(ConstantKey, binding);
        }
    }
}