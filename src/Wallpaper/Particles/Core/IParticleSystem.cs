using Particles.Settings;
using Renderer.FrameManagement;

namespace Particles.Core;

public interface IParticleSystem : IDisposable
{
    void Dispatch(FrameResource currentResource);

    void Render(FrameResource currentResource);
    void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings);
    void SwapBuffers();
};
public interface IParticleSystemFor<TSettings> where TSettings : ISettings
{
}