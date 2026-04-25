using System.Reflection.Metadata;
using Vortice.Direct3D12;


[Shader("vertex.hlsl", "vs")]
[Shader("pixel.hlsl", "ps")]
public abstract class ParticleSystem : IDisposable
{
    public sealed record InitContext
    {
        public ID3D12Device device;
        public ImmediateCommandList commandList;
        public GeometryBuffers geometryBuffers;
        public HeapAllocator heapAllocator; 
        public FrameManager frameManager;
        public CommonBuffers commmonBuffers;
        public FieldBuffers fieldBuffers;
    }
    protected bool Active;
    protected IComputePass ComputePass;

    protected GraphicPass GraphicPass;

    protected ParticleBuffers ParticleBuffers;
    protected FrameManager.ConstantKey ConstantKey;

    public virtual void Dispatch(FrameResource currentResource)
        => ComputePass.DispatchParticles(currentResource, ConstantKey);
    
    public void Render(FrameResource currentResource)
        => GraphicPass.Render(currentResource, ConstantKey);
    
    public abstract void UpdateConstantBuffers(FrameResource currentResource, ParticleSystems.SystemSettings systemSettings);
    public virtual void SwapBuffers()
        => ParticleBuffers.SwapBuffers();

    protected void ConstructRequiredFields(InitContext context, uint bufferSize, string name, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        ParticleBuffers = new ParticleBuffers(context.device, context.commandList, context.heapAllocator, name, bufferSize);
        ConstructPass(context, name, compute, precompute, vertex, pixel);
    }
    protected void ConstructRequiredFields(InitContext context, Particle[] initParticles, string name, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        ParticleBuffers = new ParticleBuffers(context.device, context.commandList, context.heapAllocator, name, initParticles);
        ConstructPass(context, name, compute, precompute, vertex, pixel);
    }

    private void ConstructPass(InitContext context, string name, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        GraphicPass = new GraphicPass(context.device, ParticleBuffers, context.commmonBuffers, context.geometryBuffers, vertex, pixel);
        ComputePass = new ComputePass(context.device, ParticleBuffers, context.commmonBuffers, context.fieldBuffers, compute, precompute);
        ConstantKey = context.frameManager.ReserveBuffer();
    }
    
    public abstract void InitBuffer(FrameResource frameResource, ID3D12Device device);
    public void Dispose()
    {
        GraphicPass?.Dispose();
        ComputePass?.Dispose();
        ParticleBuffers?.Dispose();
    }
}