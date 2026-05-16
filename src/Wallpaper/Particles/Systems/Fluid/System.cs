using Particles.Core;
using Particles.Passes;
using Particles.Resources;
using Particles.Settings;
using Renderer.FrameManagement;
using Renderer.Resources;

namespace Particles.Systems.Fluid;

public sealed class ParticleSystem : IParticleSystem, IParticleSystemFor<Settings>
{
    private readonly ConstantBufferKey<Constants> ConstantKey;
    private readonly ParticleComputeBindings ParticleComputeBindings;
    private readonly Graphic graphic;
    private readonly Controller controller;
    private readonly DensityDebugPass debugPass;
    private readonly FluidCompute fluidCompute;
    private readonly ParticleBuffers ParticleBuffers;
    private readonly FluidBuffers fluidBuffers;

    public ParticleSystem(ParticleSystemInitContext context, Settings settings)
    {
        ConstantKey = context.Registry.Reserve<Constants>("FluidSystem_Constant");
        ParticleBuffers = new ParticleBuffers(context.Device, context.CommandList, context.HeapAllocator, "Fluid", (uint)settings.initSettings.MaxParticleAmount);
        controller = new Controller(ParticleBuffers);
        
        graphic = new Graphic(context.Device, ParticleBuffers, context.CommonBuffers, context.GeometryBuffers, "vertex.hlsl","pixel.hlsl");

        ParticleComputeBindings = new ParticleComputeBindings(
            context.Device, context.HeapAllocator,
             ParticleBuffers, context.CommonBuffers, 
             context.FieldBuffers, context.CommandList, 
             "Fluid", ParticleBuffers.particleCount);

        fluidBuffers = new FluidBuffers(context.Device, context.CommandList, ParticleBuffers.particleCount);
        var fluidUavs = fluidBuffers.CreateUavTable(context.Device, context.HeapAllocator);
        var fluidSrvs = fluidBuffers.CreateSrvTable(context.Device, context.HeapAllocator);

        fluidCompute = new FluidCompute(context.Device, ParticleComputeBindings, ParticleBuffers.particleCount, fluidBuffers, fluidUavs[0].Gpu, fluidSrvs[0].Gpu, ConstantKey);
        debugPass = new DensityDebugPass(context.Device, ParticleComputeBindings, fluidSrvs[0].Gpu, context.CommonBuffers, "fluid\\density_debug_shader.hlsl");
    }

    [SystemBuilder]
    public static ParticleSystem? Create(ParticleSystemInitContext context, Settings settings)
    {
        if (settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new ParticleSystem(context, settings);
    }
    private ref Constants Constants(FrameResource currentResource)
    {
        return ref currentResource.GetBufferConstantRef<Constants>(ConstantKey);
    }
    public void Render(FrameResource currentResource)
    {
        if (Constants(currentResource).Settings.DensityDebug > 0.5f)
            debugPass.Render(currentResource, ConstantKey);
        
        graphic.Render(currentResource, ConstantKey);
    }

    public void UpdateConstantBuffers(FrameResource currentResource)
    {
        controller.UpdateStaticResource(
            ref Constants(currentResource),
            currentResource.FrameMetric,
            currentResource.Settings);
    }

    public void Dispose()
    {
        fluidBuffers.Dispose();
        ParticleComputeBindings.Dispose();
        graphic.Dispose();
        debugPass.Dispose();
        fluidCompute.Dispose();
        ParticleBuffers.Dispose();
    }

    public void Dispatch(FrameResource currentResource)
    {
        float subpasses = currentResource.Settings.GetSettings<Settings>().Subdivides;
        if (subpasses < 1)
        {
            Serilog.Log.Error("Invalid subpasses settings in {@FluidSystem}, skipping dispatch", this);
            return;
        }
        fluidCompute.DispatchParticles(currentResource, (uint)subpasses);
    }

}
