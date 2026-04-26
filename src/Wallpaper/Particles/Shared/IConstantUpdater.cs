
using Particles.Settings;
using Renderer.Core;
using Renderer.FrameManagement;

namespace Particles.Shared;
public interface IConstantUpdater
{
    void UpdateConstants(FrameResource currentResource, SystemSettings systemSettings);
}