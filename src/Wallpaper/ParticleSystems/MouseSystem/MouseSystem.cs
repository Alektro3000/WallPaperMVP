
using Vortice.Direct3D12;


public class MouseSystem : ParticleSystem
{
    protected MouseController ParticleSystemController;
    protected MouseBuffer mouseBuffer;
    public MouseSystem(InitContext context)
    {
        uint particleCount = 4096;

        ParticleBuffers = new ParticleBuffers(context.device, context.commandList, context.HeapAllocator, particleCount);
        GraphicPass = new GraphicPass(context.device, ParticleBuffers, context.commmonBuffers, context.GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        mouseBuffer = new MouseBuffer(context.device, context.HeapAllocator, ParticleBuffers, particleCount);
        ComputePass = new MouseCompute(context.device, ParticleBuffers, mouseBuffer, context.commmonBuffers, context.fieldBuffers);
        ConstantKey = context.FrameManager.ReserveBuffer();

        //ParticleBuffers = new ParticleBuffers(context.device, context.commandList, context.HeapAllocator, initParticles);
        ParticleSystemController = new MouseController(ParticleBuffers);
        
        Serilog.Log.Information("Mouse System Initialized");
    }
    public override void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings)
    {
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<MouseConstants>(ConstantKey),
        currentResource.frameMetric, systemSettings);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<MouseConstants>(device));
    }
    public override void SwapBuffers(){}
}