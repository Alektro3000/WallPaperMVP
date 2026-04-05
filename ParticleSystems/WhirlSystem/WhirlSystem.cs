
using Vortice.Direct3D12;

public class WhirlSystem : ParticleSystem
{
    protected WhirlController ParticleSystemController;
    public WhirlSystem(
        ID3D12Device device, 
        ImmediateCommandList commandList, 
        GeometryBuffers GeometryBuffers, 
        HeapAllocator HeapAllocator, 
        FrameManager FrameManager)
    {
        ParticleBuffers = new ParticleBuffers(device, commandList, HeapAllocator, 3000);
        GraphicPass = new GraphicPass(device, ParticleBuffers, GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        ComputePass = new ComputePass(device, ParticleBuffers, "whirl/compute.hlsl", "whirl/precompute.hlsl");
        ParticleSystemController = new WhirlController(ParticleBuffers);
        ConstantKey = FrameManager.ReserveBuffer();
    }

    public override void UpdateStaticResource(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBuffer(ConstantKey).Constants<WhirlConstants>(),
            currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {        
        unsafe
        {
            var constantBuffer = BufferHelper.CreateStaticBuffer(device, out WhirlConstants* MappedConstants);
            
            var binding = new FrameResource.ConstantBinding
            {
                ConstantBuffer = constantBuffer,
                MappedConstants = (byte*)MappedConstants,
            };
            
            frameResource.AddBuffer(ConstantKey, binding);
        }
    }
}