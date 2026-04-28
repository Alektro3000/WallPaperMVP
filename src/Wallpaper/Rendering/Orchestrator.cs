
using Particles.Systems;
using Renderer.Core;
using Serilog;
using SharedField = Particles.Shared.Field;
using SharedCommon = Particles.Shared.Global;
using Renderer;
using Renderer.Descriptors;
using Particles.Settings;
using Particles.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Commands;
using Particles.Shared;

namespace Renderer;
public sealed class Orchestrator : IDisposable
{
    //SubClasses
    private readonly GraphicsContext Context;
    private readonly FrameManager FrameManager;
    private readonly HeapAllocator HeapAllocator;
    private readonly GeometryBuffers GeometryBuffers;
    private readonly ConstantBufferRegistry ConstantBufferRegistry;
    private readonly IConstantBufferSet[] constantBufferSets;
    private readonly IConstantUpdater[] constantUpdaters;

    private readonly SharedField.Pass fieldPass;

    public readonly IParticleSystem[] ParticleSystems;
    public Orchestrator(SystemSettings systemSettings, IntPtr hwnd, int width, int height)
    {
        Context = new GraphicsContext();
        Log.Debug("Graphic Context initialized");

        using var commandList = new ImmediateCommandList(Context);
        Log.Debug("CommandList initialized");

        HeapAllocator = new HeapAllocator(Context.Device);
        GeometryBuffers = new GeometryBuffers(Context.Device, commandList, HeapAllocator);


        ConstantBufferRegistry = new ConstantBufferRegistry();

        var field = 
            new SharedField.Buffers(Context.Device, ConstantBufferRegistry , HeapAllocator);
        var common = 
            new SharedCommon.Buffers(ConstantBufferRegistry );
        
        constantBufferSets = [
            field,
            common
        ];
        
        constantUpdaters = [
            new SharedField.Controller(field),
            new SharedCommon.Controller(common)
        ];

        fieldPass = new SharedField.Pass(Context.Device, field, common, "field.hlsl");
        Log.Information("FieldPass Initialized");


        ParticleSystemInitContext context = new()
        {
            Device = Context.Device,
            CommandList = commandList,
            GeometryBuffers = GeometryBuffers,
            HeapAllocator = HeapAllocator,
            CommonBuffers = common,
            FieldBuffers = field,
            Registry = ConstantBufferRegistry
        };
        
        ParticleSystems = ParticleSystemReflection.CreateParticleSystems(systemSettings, context).ToArray();
        
        
        FrameManager = new FrameManager(Context, hwnd, width, height, HeapAllocator, ConstantBufferRegistry);
        Log.Information("FrameManager Initialized {@FrameManager}", FrameManager);
        

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
    }


    public void Dispose()
    {
        
        FrameManager.WaitForAllFrames();

        foreach (var item in ParticleSystems)
            item.Dispose();

        fieldPass.Dispose();
        
        foreach (var item in constantBufferSets)
            item.Dispose();

        GeometryBuffers.Dispose();
        FrameManager.Dispose();
        HeapAllocator.Dispose();
        Context.Dispose();
    }
}