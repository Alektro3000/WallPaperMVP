using Vortice.Direct3D12;
using SharedField = Particles.Shared.Field;
using SharedCommon = Particles.Shared.Global;
using Renderer;
using Renderer.Passes;
using Renderer.Descriptors;
using Particles.Settings;
using Renderer.Core;
using Particles.Passes;
using Particles.Resources;
using Renderer.Resources;
using Renderer.FrameManagement;
using Renderer.Commands;

namespace Particles.Core;

[Shader("vertex.hlsl", "vs")]
[Shader("pixel.hlsl", "ps")]
[Shader("shared\\alive.hlsl", "cs")]
[Shader("shared\\prefix_local.hlsl", "cs")]
[Shader("shared\\prefix_block_sums.hlsl", "cs")]
[Shader("shared\\prefix_add_offset.hlsl", "cs")]
[Shader("shared\\copy.hlsl", "cs")]
[Shader("shared\\draw_count_no_compact.hlsl", "cs")]
public abstract class BaseParticleSystem : IParticleSystem, IDisposable
{
    protected Compute ComputePass;

    protected Graphic GraphicPass;

    protected ParticleBuffers ParticleBuffers;

    protected ParticleComputeBindings ParticleComputeBindings;

    protected ConstantBufferKey ConstantKey;

    public virtual void Dispatch(FrameResource currentResource)
        => ComputePass.DispatchParticles(currentResource, ConstantKey, currentResource.frameMetric.FrameIndex % 10 == 0);

    public virtual void Render(FrameResource currentResource)
        => GraphicPass.Render(currentResource, ConstantKey);

    protected BaseParticleSystem(
        ParticleSystemInitContext context,
        uint bufferSize,
        string name)
    {
        ParticleBuffers = new ParticleBuffers(context.Device, context.CommandList, context.HeapAllocator, name, bufferSize);

        ParticleComputeBindings = new ParticleComputeBindings(context.Device, 
                context.HeapAllocator, 
                ParticleBuffers, 
                context.CommonBuffers, 
                context.FieldBuffers, 
                context.CommandList, name, bufferSize);

        GraphicPass = new Graphic(context.Device, ParticleBuffers, context.CommonBuffers, context.GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        ComputePass = new Compute(context.Device, ParticleComputeBindings,
            name.ToLower() + "\\compute.hlsl", 
            name.ToLower() + "\\emitter.hlsl", 
            name.ToLower() + "\\draw_count.hlsl");

        Serilog.Log.Information("Particle system {ParticleSystem} with name {name} initialized ", this, name);
    }
    protected BaseParticleSystem(
        ParticleSystemInitContext context,
        Particle[] initParticles,
        string name)
    {
        uint bufferSize = (uint)initParticles.Length;
        ParticleBuffers = new ParticleBuffers(context.Device, context.CommandList, context.HeapAllocator, name, initParticles);
        
        ParticleComputeBindings = new ParticleComputeBindings(context.Device, 
                context.HeapAllocator, 
                ParticleBuffers, 
                context.CommonBuffers, 
                context.FieldBuffers, 
                context.CommandList, name, bufferSize);

        GraphicPass = new Graphic(context.Device, ParticleBuffers, context.CommonBuffers, context.GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        ComputePass = new Compute(context.Device, ParticleComputeBindings,
            name.ToLower() + "\\compute.hlsl", 
            name.ToLower() + "\\emitter.hlsl", 
            name.ToLower() + "\\draw_count.hlsl");
            
        Serilog.Log.Information("Particle system {ParticleSystem} with name {name} initialized ", this, name);
    }

    public void Dispose()
    {
        ParticleComputeBindings?.Dispose();
        GraphicPass?.Dispose();
        ComputePass?.Dispose();
        ParticleBuffers?.Dispose();
    }

    public abstract void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings);
}