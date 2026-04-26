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
public abstract class BaseParticleSystem : IDisposable
{
    protected ICompute ComputePass;

    protected Graphic GraphicPass;

    protected ParticleBuffers ParticleBuffers;
    
    protected ConstantBufferKey ConstantKey;

    

    public virtual void Dispatch(FrameResource currentResource)
        => ComputePass.DispatchParticles(currentResource, ConstantKey);
    
    public void Render(FrameResource currentResource)
        => GraphicPass.Render(currentResource, ConstantKey);
    
    public abstract void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings);
    public virtual void SwapBuffers()
        => ParticleBuffers.SwapBuffers();

    protected void ConstructRequiredFields(ParticleSystemInitContext context, uint bufferSize, string name, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        ParticleBuffers = new ParticleBuffers(context.Device, context.CommandList, context.HeapAllocator, name, bufferSize);
        ConstructPass(context, name, compute, precompute, vertex, pixel);
    }
    protected void ConstructRequiredFields(ParticleSystemInitContext context, Particle[] initParticles, string name, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        ParticleBuffers = new ParticleBuffers(context.Device, context.CommandList, context.HeapAllocator, name, initParticles);
        ConstructPass(context, name, compute, precompute, vertex, pixel);
    }

    private void ConstructPass(ParticleSystemInitContext context, string name, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        GraphicPass = new Graphic(context.Device, ParticleBuffers, context.CommonBuffers, context.GeometryBuffers, vertex, pixel);
        ComputePass = new Compute(context.Device, ParticleBuffers, context.CommonBuffers, context.FieldBuffers, compute, precompute);
        Serilog.Log.Information("Particle system {ParticleSystem} with name {name} initialized ", this, name);
    }
    
    public void Dispose()
    {
        GraphicPass?.Dispose();
        ComputePass?.Dispose();
        ParticleBuffers?.Dispose();
    }
}