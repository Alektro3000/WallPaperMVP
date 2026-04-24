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
        public GeometryBuffers GeometryBuffers;
        public HeapAllocator HeapAllocator; 
        public FrameManager FrameManager;
        public CommonBuffers commmonBuffers;
        public FieldBuffers fieldBuffers;
    }
    
    protected IComputePass ComputePass;

    protected GraphicPass GraphicPass;

    protected ParticleBuffers ParticleBuffers;
    protected FrameManager.ConstantKey ConstantKey;

    public virtual void Dispatch(FrameResource currentResource)
        => ComputePass.DispatchParticles(currentResource, ConstantKey);
    
    public void Render(FrameResource currentResource)
        => GraphicPass.Render(currentResource, ConstantKey);
    
    public abstract void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings);
    public virtual void SwapBuffers()
        => ParticleBuffers.SwapBuffers();

    protected void ConstructRequiredFields(InitContext context, uint bufferSize, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        ParticleBuffers = new ParticleBuffers(context.device, context.commandList, context.HeapAllocator, bufferSize);
        ConstructPass(context, compute, precompute, vertex, pixel);
    }
    protected void ConstructRequiredFields(InitContext context, Particle[] initParticles, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        ParticleBuffers = new ParticleBuffers(context.device, context.commandList, context.HeapAllocator, initParticles);
        ConstructPass(context, compute, precompute, vertex, pixel);
    }

    private void ConstructPass(InitContext context, string compute, string precompute, string vertex = "vertex.hlsl", string pixel = "pixel.hlsl")
    {
        GraphicPass = new GraphicPass(context.device, ParticleBuffers, context.commmonBuffers, context.GeometryBuffers, vertex, pixel);
        ComputePass = new ComputePass(context.device, ParticleBuffers,  context.commmonBuffers, context.fieldBuffers, compute, precompute);
        ConstantKey = context.FrameManager.ReserveBuffer();
    }
    
    public abstract void InitBuffer(FrameResource frameResource, ID3D12Device device);
    public void Dispose()
    {
        GraphicPass?.Dispose();
        ComputePass?.Dispose();
        ParticleBuffers?.Dispose();
    }
}