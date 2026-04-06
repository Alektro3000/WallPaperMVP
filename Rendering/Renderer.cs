
public sealed class Renderer : IDisposable
{
    //SubClasses
    private readonly GraphicsContext Context;
    private readonly FrameManager FrameManager;
    private readonly HeapAllocator HeapAllocator;
    private readonly GeometryBuffers GeometryBuffers;

    public readonly ParticleSystem[] ParticleSystems;
    public Renderer(IntPtr hwnd, int width, int height)
    {
        Context = new GraphicsContext();
        using var commandList = new ImmediateCommandList(Context);

        HeapAllocator = new HeapAllocator(Context.Device);
        FrameManager = new FrameManager(Context, hwnd, width, height, HeapAllocator);
        GeometryBuffers = new GeometryBuffers(Context.Device, commandList, HeapAllocator);
        ParticleSystems = [
            new MouseSystem(Context.Device, commandList, GeometryBuffers, HeapAllocator, FrameManager),
            new WhirlSystem(Context.Device, commandList, GeometryBuffers, HeapAllocator, FrameManager),
            new CornerSystem(Context.Device, commandList, GeometryBuffers, HeapAllocator, FrameManager),
            new StripSystem(Context.Device, commandList, GeometryBuffers, HeapAllocator, FrameManager),
            new TextSystem(Context.Device, commandList, GeometryBuffers, HeapAllocator, FrameManager),
        ];
        FrameManager.PopulateConstantBuffers();
        FrameManager.ExecuteForEachFrame(x =>
        {
            foreach (var item in ParticleSystems)
            {
                item.InitBuffer(x, Context.Device);
            }
        });

        //renderer2D = new Renderer2DPass(Context.Device, Context.CommandQueue);
    }

    public void Render()
    {
        var currentResource = FrameManager.BeginFrame();
        foreach (var item in ParticleSystems)
            item.UpdateStaticResource(currentResource);

        foreach (var item in ParticleSystems)
            item.Dispatch(currentResource);

        foreach (var item in ParticleSystems)
            item.Render(currentResource);
            

        FrameManager.EndFrame(currentResource);
        
        foreach (var item in ParticleSystems)
            item.SwapBuffers();
    }


    public void Dispose()
    {
        FrameManager.WaitForAllFrames();

        GeometryBuffers.Dispose();
        HeapAllocator.Dispose();

        foreach (var item in ParticleSystems)
            item.Dispose();
        FrameManager.Dispose();
        Context.Dispose();

    }
}