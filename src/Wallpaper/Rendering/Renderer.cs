
using ParticleSystems;
using Serilog;

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
    public Renderer(SystemSettings systemSettings, IntPtr hwnd, int width, int height)
    {
        Context = new GraphicsContext();
        Log.Debug("Graphic Context initialized");

        using var commandList = new ImmediateCommandList(Context);
        Log.Debug("CommandList initialized");

        HeapAllocator = new HeapAllocator(Context.Device);

        FrameManager = new FrameManager(Context, hwnd, width, height, HeapAllocator);
        Log.Information("FrameManager Initialized {@FrameManager}", FrameManager);

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
        Log.Information("FieldPass Initialized");

        debugPass = new DebugPass(Context.Device, field.SRVFieldDescriptor);
        Log.Information("debugPass Initialized");


        ParticleSystem.InitContext context = new ParticleSystem.InitContext
        {
            device = Context.Device,
            commandList = commandList,
            geometryBuffers = GeometryBuffers,
            heapAllocator = HeapAllocator,
            frameManager = FrameManager,
            commmonBuffers = common,
            fieldBuffers = field,
        };
        
        ParticleSystems = ParticleSystemReflection.CreateParticleSystems(systemSettings, context).ToArray();
        
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

    public void Render(SystemSettings systemSettings)
    {
        var currentResource = FrameManager.BeginFrame();

        foreach (var item in constantUpdaters)
            item.UpdateConstants(currentResource, systemSettings);

        fieldPass.UpdateField(currentResource);

        foreach (var item in ParticleSystems)
            item.UpdateConstantBuffers(currentResource, systemSettings);

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