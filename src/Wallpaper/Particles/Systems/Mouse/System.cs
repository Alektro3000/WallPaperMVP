
using Particles.Core;
using Particles.Passes;
using Particles.Resources;
using Particles.Settings;
using Particles.Shared.Field;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Passes;
using Renderer.Resources;
using Vortice.Direct3D12;


namespace Particles.Systems.Mouse;
public sealed class ParticleSystem : IParticleSystem, IParticleSystem<Settings>
{
    private Controller ParticleSystemController;
    private Compute mouseCompute;
    private Buffer mouseBuffer;
    

    private Graphic GraphicPass;

    private ParticleBuffers ParticleBuffers;

    private ConstantBufferKey ConstantKey;
    
    public ParticleSystem(ParticleSystemInitContext context, Settings settings)
    {
        uint particleCount = Math.Min(65536, (uint)settings.initSettings.MaxParticleAmount);

        ParticleBuffers = new ParticleBuffers(context.Device, context.CommandList, context.HeapAllocator, "MouseSystem", particleCount);
        GraphicPass = new Graphic(context.Device, ParticleBuffers, context.CommonBuffers, context.GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        mouseBuffer = new Buffer(context.Device, context.HeapAllocator, ParticleBuffers, particleCount);
        mouseCompute = new Compute(context.Device, ParticleBuffers, mouseBuffer, context.CommonBuffers, context.FieldBuffers);
        ConstantKey = context.Registry.Reserve(device => BufferFactory.CreateConstantBuffer<Constants>(device, "MouseSystem_Constant"));

        ParticleSystemController = new Controller(ParticleBuffers);
        
        Serilog.Log.Information("Particle system {ParticleSystem} with name {name} initialized ", this, "MouseSystem");
    }

    [SystemBuilder]
    public static ParticleSystem? Create(ParticleSystemInitContext context, Settings settings)
    {
        if(settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new ParticleSystem(context, settings);
    }
    public void Dispatch(FrameResource currentResource)
    {
        bool compact = currentResource.FrameIndex % 1 == 0;
        mouseCompute.DispatchParticles(currentResource, ConstantKey, compact);
        if(!compact)
            ParticleBuffers.SwapBuffers();
    }
    public void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings) =>
        ParticleSystemController.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<Constants>(ConstantKey),
        currentResource.frameMetric, systemSettings);
    

    public void Render(FrameResource currentResource) => GraphicPass.Render(currentResource, ConstantKey);

    public void SwapBuffers(){}

    public void Dispose()
    {
        mouseCompute.Dispose();
        mouseBuffer.Dispose();
        GraphicPass.Dispose();
        ParticleBuffers.Dispose();
    }
}