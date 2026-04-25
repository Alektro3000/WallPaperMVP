
using Vortice.Direct3D12;


namespace ParticleSystems.Mouse;
public class System : ParticleSystem, IParticleSystem<Settings>
{
    protected Controller ParticleSystemController;
    protected Compute mouseCompute;
    protected Buffer mouseBuffer;
    public System(InitContext context, Settings settings)
    {
        uint particleCount = Math.Min(65536, (uint)settings.initSettings.MaxParticleAmount);

        ParticleBuffers = new ParticleBuffers(context.device, context.commandList, context.heapAllocator, "MouseSystem", particleCount);
        GraphicPass = new GraphicPass(context.device, ParticleBuffers, context.commmonBuffers, context.geometryBuffers, "vertex.hlsl", "pixel.hlsl");
        mouseBuffer = new Buffer(context.device, context.heapAllocator, ParticleBuffers, particleCount);
        mouseCompute = new Compute(context.device, ParticleBuffers, mouseBuffer, context.commmonBuffers, context.fieldBuffers);
        ConstantKey = context.frameManager.ReserveBuffer();

        ParticleSystemController = new Controller(ParticleBuffers);
        
        Serilog.Log.Information("Mouse System Initialized");
    }

    [SystemBuilder]
    public static System? Create(InitContext context, Settings settings)
    {
        if(settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new System(context, settings);
    }
    public override void Dispatch(FrameResource currentResource)
    {
        bool compact = currentResource.FrameIndex % 1 == 0;
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
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<Constants>(device, "MouseSystem_Constant"));
    }
    public override void SwapBuffers()
    {
        
    }
}