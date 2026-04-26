
using Renderer.Core;
using Renderer.FrameManagement;
using Vortice.Direct3D12;

namespace Renderer.Passes;

public interface ICompute : IDisposable
{
    void DispatchParticles(FrameResource currentResource, ConstantBufferKey key);
}