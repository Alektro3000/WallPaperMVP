
using Vortice.Direct3D12;

namespace MouseSystem;

public class MouseSystem : ParticleSystem
{
    protected Controller ParticleSystemController;
    protected Compute mouseCompute;
    protected Buffer mouseBuffer;
    public MouseSystem(InitContext context)
    {
        uint particleCount = 65536;

        ParticleBuffers = new ParticleBuffers(context.device, context.commandList, context.HeapAllocator, particleCount);
        GraphicPass = new GraphicPass(context.device, ParticleBuffers, context.commmonBuffers, context.GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        mouseBuffer = new Buffer(context.device, context.HeapAllocator, ParticleBuffers, particleCount);
        mouseCompute = new Compute(context.device, ParticleBuffers, mouseBuffer, context.commmonBuffers, context.fieldBuffers);
        ConstantKey = context.FrameManager.ReserveBuffer();

        ParticleSystemController = new Controller(ParticleBuffers);
        
        Serilog.Log.Information("Mouse System Initialized");
    }
    public override void Dispatch(FrameResource currentResource)
    {
        bool compact = currentResource.FrameIndex % 10 == 0;
        mouseCompute.DispatchParticles(currentResource, ConstantKey, compact);
        if(!compact)
            ParticleBuffers.SwapBuffers();
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
    public override void SwapBuffers()
    {
        
    }
}