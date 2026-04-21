
using Vortice.Direct3D12;

public interface IComputePass : IDisposable
{
    void DispatchParticles(FrameResource currentResource, FrameManager.ConstantKey key);


}