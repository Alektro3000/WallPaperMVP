using Particles.Settings;
using Renderer.FrameManagement;
using Settings;

namespace Particles.Core;

public interface IParticleSystem : IDisposable
{
    void Dispatch(FrameResource currentResource);

    void Render(FrameResource currentResource);
    void UpdateConstantBuffers(FrameResource currentResource);

};
public interface IParticleSystemFor<TSettings> where TSettings : ISettings
{
}