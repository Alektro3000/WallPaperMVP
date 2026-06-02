
using Models;
using Models.Loader;
using Renderer.FrameManagement;

class ModelSubsystem : IDisposable
{

    Model[] models;
    public ModelSubsystem(InitContext initContext)
    {
        var roomSettings = initContext.SystemSettings.GetSettings<Models.Settings>().loadRoom > 0.5f;
        models = 
        [
            roomSettings ? ModelLoader.loadModelFromGLTF(initContext, "room2", "room.gltf")
            : ModelLoader.loadModelFromGLTF(initContext, "room", "l1.gltf")
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