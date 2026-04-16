
public sealed class Renderer : IDisposable
{
    //SubClasses
    private readonly GraphicsContext Context;
    private readonly FrameManager FrameManager;
    private readonly HeapAllocator HeapAllocator;
    private readonly GeometryBuffers GeometryBuffers;

    private readonly IConstantBufferSet[] constantBufferSets;
    private readonly IConstantUpdater[] constantUpdaters;

    private readonly FieldPass fieldPass;

    private readonly DebugPass debugPass;
    public readonly ParticleSystem[] ParticleSystems;
    public Renderer(IntPtr hwnd, int width, int height, SystemSettings systemSettings)
    {
        Context = new GraphicsContext();
        using var commandList = new ImmediateCommandList(Context);

        HeapAllocator = new HeapAllocator(Context.Device);
        FrameManager = new FrameManager(Context, hwnd, width, height, HeapAllocator);
        GeometryBuffers = new GeometryBuffers(Context.Device, commandList, HeapAllocator);
        var field = 
            new FieldBuffers(Context.Device, FrameManager, HeapAllocator);
        var common = 
            new CommonBuffers(FrameManager);
        
        constantBufferSets = [
            field,
            common
        ];
        
        constantUpdaters = [
            new FieldUpdater(field),
            new CommonUpdater(common)
        ];

        fieldPass = new FieldPass(Context.Device, field, common, "field.hlsl");
        debugPass = new DebugPass(Context.Device, field.SRVFieldDescriptor);

        ParticleSystem.InitContext context = new ParticleSystem.InitContext
        {
            device = Context.Device,
            commandList = commandList,
            GeometryBuffers = GeometryBuffers,
            HeapAllocator = HeapAllocator,
            FrameManager = FrameManager,
            commmonBuffers = common,
            fieldBuffers = field,
            systemSettings = systemSettings
        };
        
        ParticleSystems = [
            new MouseSystem(context),
            new WhirlSystem(context),
            new CornerSystem(context),
            new StripSystem(context),
            new TextSystem(context),
        ];
        FrameManager.PopulateConstantBuffers();
        FrameManager.ExecuteForEachFrame(x =>
        {
            foreach (var item in constantBufferSets)
                item.InitBuffers(x, Context.Device);

            foreach (var item in ParticleSystems)
                item.InitBuffer(x, Context.Device);
        });

        //renderer2D = new Renderer2DPass(Context.Device, Context.CommandQueue);
    }

    public void Render()
    {
        var currentResource = FrameManager.BeginFrame();

        foreach (var item in constantUpdaters)
            item.UpdateConstants(currentResource);

        fieldPass.UpdateField(currentResource);

        foreach (var item in ParticleSystems)
            item.UpdateConstantBuffers(currentResource);

        foreach (var item in ParticleSystems)
            item.Dispatch(currentResource);

        foreach (var item in ParticleSystems)
            item.Render(currentResource);
            
        //debugPass.Render(currentResource);
        FrameManager.EndFrame(currentResource);
        
        foreach (var item in ParticleSystems)
            item.SwapBuffers();
    }


    public void Dispose()
    {
        
        FrameManager.WaitForAllFrames();

        foreach (var item in ParticleSystems)
            item.Dispose();

        debugPass.Dispose();
        fieldPass.Dispose();
        
        foreach (var item in constantBufferSets)
            item.Dispose();

        GeometryBuffers.Dispose();
        FrameManager.Dispose();
        HeapAllocator.Dispose();
        Context.Dispose();
    }
}