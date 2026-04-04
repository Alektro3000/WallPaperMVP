
public sealed class Renderer : IDisposable
{
    //SubClasses
    private readonly GraphicsContext Context;
    private readonly FrameManager FrameManager;
    private readonly HeapAllocator HeapAllocator;

    private readonly ComputePass ComputePass;
    private readonly GraphicPass GraphicPass;
    private readonly Renderer2DPass renderer2D;
    
    private readonly ParticleBuffers ParticleBuffers;
    private readonly GeometryBuffers GeometryBuffers;
    private readonly ParticleSimulation ParticleSystemController;

    public Renderer(IntPtr hwnd, int width, int height)
    {
        Context = new GraphicsContext();
        using var commandList = new ImmediateCommandList(Context);

        HeapAllocator = new HeapAllocator(Context.Device);
        ParticleBuffers = new ParticleBuffers(Context.Device, commandList, HeapAllocator);
        FrameManager = new FrameManager(Context, hwnd, width, height, HeapAllocator);
        GeometryBuffers = new GeometryBuffers(Context.Device, commandList, HeapAllocator);

        GraphicPass = new GraphicPass(Context.Device, ParticleBuffers, GeometryBuffers);
        ComputePass = new ComputePass(Context.Device, ParticleBuffers);
        renderer2D = new Renderer2DPass(Context.Device, Context.CommandQueue);
        FrameManager.ExecuteForEachFrame(x => renderer2D.InitBuffer(x));
        ParticleSystemController = new ParticleSimulation(ParticleBuffers, width, height);
    }

    public void Render()
    {
        var currentResource = FrameManager.BeginFrame();

        ParticleSystemController.UpdateStaticResource(ref currentResource.Constants);
        ComputePass.DispatchParticles(currentResource);
        GraphicPass.Render(currentResource);
        FrameManager.ExecuteFrame(currentResource);

        renderer2D.Render(currentResource);
        FrameManager.PresentFrame(currentResource);

        ParticleBuffers.SwapBuffers();
    }

    public void UpdateSimulationSettings()
    {
        
    }

    public void Dispose()
    {
        FrameManager.WaitForAllFrames();


        GraphicPass.Dispose();
        ComputePass.Dispose();
        ParticleBuffers.Dispose();
        FrameManager.Dispose();
        Context.Dispose();

    }
}