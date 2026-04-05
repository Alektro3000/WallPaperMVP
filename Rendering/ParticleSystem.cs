

using System.Reflection.Metadata;
using Vortice.Direct3D12;

public abstract class ParticleSystem : IDisposable
{
    protected ComputePass ComputePass;

    protected GraphicPass GraphicPass;

    protected ParticleBuffers ParticleBuffers;
    protected FrameManager.ConstantKey ConstantKey;

    public void Dispatch(FrameResource currentResource)
    {
        ComputePass.DispatchParticles(currentResource, ConstantKey);
    }
    public void Render(FrameResource currentResource)
    {
        GraphicPass.Render(currentResource, ConstantKey);
    }
    public abstract void UpdateStaticResource(FrameResource currentResource);
    public void SwapBuffers()
    {
        ParticleBuffers.SwapBuffers();
    }
    public abstract void InitBuffer(FrameResource frameResource, ID3D12Device device);
    public void Dispose()
    {
        GraphicPass.Dispose();
        ComputePass.Dispose();
        ParticleBuffers.Dispose();
    }
}