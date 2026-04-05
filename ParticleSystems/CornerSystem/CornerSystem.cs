
using Vortice.Direct3D12;

public class CornerSystem : ParticleSystem
{
    protected CornerController ParticleSystemController;
    public CornerSystem(
        ID3D12Device device, 
        ImmediateCommandList commandList, 
        GeometryBuffers GeometryBuffers, 
        HeapAllocator HeapAllocator, 
        FrameManager FrameManager)
    {
        ParticleBuffers = new ParticleBuffers(device, commandList, HeapAllocator, 3000);
        GraphicPass = new GraphicPass(device, ParticleBuffers, GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        ComputePass = new ComputePass(device, ParticleBuffers, "corner/compute.hlsl", "corner/precompute.hlsl");
        ParticleSystemController = new CornerController(ParticleBuffers);
        ConstantKey = FrameManager.ReserveBuffer();
    }

    public override void UpdateStaticResource(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBuffer(ConstantKey).Constants<CornerConstants>(),
            currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {        
        unsafe
        {
            var constantBuffer = BufferHelper.CreateStaticBuffer(device, out CornerConstants* MappedConstants);
            
            var binding = new FrameResource.ConstantBinding
            {
                ConstantBuffer = constantBuffer,
                MappedConstants = (byte*)MappedConstants,
            };
            
            frameResource.AddBuffer(ConstantKey, binding);
        }
    }
}