
using Models;
using Renderer.FrameManagement;

class ModelSubsystem : IDisposable
{

    Model model;
    public ModelSubsystem(InitContext initContext)
    {
        model = ModelLoader.loadModelFromGLTF(initContext, "room", "theroom.gltf");
    }

    public void Render(FrameResource currentResource)
    {
        model.Render(currentResource);
    }
    public void Dispose()
    {
        model.Dispose();
    }
}