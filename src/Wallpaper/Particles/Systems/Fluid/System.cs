using Particles.Core;
using Particles.Passes;
using Particles.Resources;
using Particles.Settings;
using Renderer.FrameManagement;
using Renderer.Resources;

namespace Particles.Systems.Fluid;

[Shader("fluid\\compute.hlsl", "cs")]
[Shader("fluid\\emitter.hlsl", "cs")]
[Shader("fluid\\draw_count.hlsl", "cs")]
[Shader("fluid\\sort_hash.hlsl", "cs")]
[Shader("fluid\\clear_grid.hlsl", "cs")]
[Shader("fluid\\build_ranges.hlsl", "cs")]
[Shader("fluid\\density_debug.hlsl", "vs")]
[Shader("fluid\\density_debug_pixel.hlsl", "ps")]
public sealed class ParticleSystem : IParticleSystem, IParticleSystemFor<Settings>
{
    private readonly ConstantBufferKey ConstantKey;
    private readonly ParticleComputeBindings ParticleComputeBindings;
    private readonly Graphic graphic;
    private readonly Controller controller;
    private readonly DensityDebugPass debugPass;
    private readonly FluidCompute fluidCompute;
    private readonly ParticleBuffers ParticleBuffers;
    private readonly FluidBuffers fluidBuffers;

    public ParticleSystem(ParticleSystemInitContext context, Settings settings)
    {
        ConstantKey = context.Registry.Reserve(device => BufferFactory.CreateConstantBuffer<Constants>(device, "FluidSystem_Constant"));
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

        fluidCompute = new FluidCompute(context.Device, ParticleComputeBindings, ParticleBuffers.particleCount, fluidBuffers, fluidUavs[0].Gpu, fluidSrvs[0].Gpu);
        debugPass = new DensityDebugPass(context.Device, ParticleComputeBindings, fluidSrvs[0].Gpu, context.CommonBuffers, "fluid\\density_debug.hlsl", "fluid\\density_debug_pixel.hlsl");
    }

    [SystemBuilder]
    public static ParticleSystem? Create(ParticleSystemInitContext context, Settings settings)
    {
        if (settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new ParticleSystem(context, settings);
    }

    public void Render(FrameResource currentResource)
    {
        if (currentResource.GetBufferConstantRef<Constants>(ConstantKey).Settings.DensityDebug > 0.5f)
            debugPass.Render(currentResource, ConstantKey);
        
        graphic.Render(currentResource, ConstantKey);
    }

    public void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings)
    {
        controller.UpdateStaticResource(
            ref currentResource.GetBufferConstantRef<Constants>(ConstantKey),
            currentResource.frameMetric,
            systemSettings);
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
        fluidCompute.DispatchParticles(currentResource, ConstantKey, false);
    }

}
