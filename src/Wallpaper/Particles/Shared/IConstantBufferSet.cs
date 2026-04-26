using Renderer.Core;
using Renderer.FrameManagement;
using Vortice.Direct3D12;

namespace Particles.Shared;
public interface IConstantBufferSet : IDisposable
{
    void InitBuffers(FrameResource frameResource, ID3D12Device device);
}