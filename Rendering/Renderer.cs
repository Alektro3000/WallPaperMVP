
public sealed class Renderer : IDisposable
{
    //SubClasses
    private readonly GraphicsContext Context;
    private readonly FrameManager FrameManager;
    private readonly HeapAllocator HeapAllocator;
    private readonly GeometryBuffers GeometryBuffers;

    private readonly ParticleSystem ParticleSystem;
    public Renderer(IntPtr hwnd, int width, int height)
    {
        Context = new GraphicsContext();
        using var commandList = new ImmediateCommandList(Context);

        HeapAllocator = new HeapAllocator(Context.Device);
        FrameManager = new FrameManager(Context, hwnd, width, height, HeapAllocator);
        GeometryBuffers = new GeometryBuffers(Context.Device, commandList, HeapAllocator);
        ParticleSystem = new WhirlSytem(Context.Device, commandList, GeometryBuffers, HeapAllocator, FrameManager,  width, height);
        FrameManager.PopulateConstantBuffers();
        FrameManager.ExecuteForEachFrame(x => ParticleSystem.InitBuffer(x, Context.Device));

        //renderer2D = new Renderer2DPass(Context.Device, Context.CommandQueue);
    }

    public void Render()
    {
        var currentResource = FrameManager.BeginFrame();

        ParticleSystem.UpdateStaticResource(currentResource);
        ParticleSystem.Dispatch(currentResource);
        ParticleSystem.Render(currentResource);

        FrameManager.EndFrame(currentResource);
        ParticleSystem.SwapBuffers();
    }


    public void Dispose()
    {
        FrameManager.WaitForAllFrames();


        ParticleSystem.Dispose();
        FrameManager.Dispose();
        Context.Dispose();

    }
}