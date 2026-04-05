

using System.Reflection.Metadata;
using Vortice.Direct3D12;

public abstract class ParticleSystem : IDisposable
{
    protected ComputePass ComputePass;

    protected GraphicPass GraphicPass;

    protected ParticleBuffers ParticleBuffers;
    protected ParticleController ParticleSystemController;
    protected FrameManager.ConstantKey ConstantKey;

    public void Dispatch(FrameResource currentResource)
    {
        ComputePass.DispatchParticles(currentResource, ConstantKey);
    }
    public void Render(FrameResource currentResource)
    {
        GraphicPass.Render(currentResource, ConstantKey);
    }
    public void UpdateStaticResource(FrameResource currentResource)
    {
        ParticleSystemController.UpdateStaticResource(ref currentResource.GetBuffer(ConstantKey).Constants<Constants>());
    }
    public void SwapBuffers()
    {
        ParticleBuffers.SwapBuffers();
    }
    public void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {        
        unsafe
        {
            var constantBuffer = BufferHelper.CreateStaticBuffer(device, out Constants* MappedConstants);
            
            var binding = new FrameResource.ConstantBinding
            {
                ConstantBuffer = constantBuffer,
                MappedConstants = (byte*)MappedConstants,
            };
            
            frameResource.AddBuffer(ConstantKey, binding);
        }
    }
    public void Dispose()
    {
        GraphicPass.Dispose();
        ComputePass.Dispose();
        ParticleBuffers.Dispose();
    }
}