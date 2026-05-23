
using Models;
using Renderer.FrameManagement;

class ModelSubsystem : IDisposable
{

    Model[] models;
    public ModelSubsystem(InitContext initContext)
    {
        models = 
        [
            ModelLoader.loadModelFromGLTF(initContext, "room", "room.gltf")
        ];
    }

    public void Render(FrameResource currentResource)
    {
        foreach(var model in models)
            model.Render(currentResource);
    }
    public void Dispose()
    {
        foreach(var model in models)
            model.Dispose();
    }
}