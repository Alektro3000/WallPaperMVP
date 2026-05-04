
using Renderer.Core;
using Serilog;
using SharedField = Particles.Shared.Field;
using SharedCommon = Particles.Shared.Global;
using Renderer.Descriptors;
using Particles.Settings;
using Particles.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Commands;
using Particles.Shared;
using System.Threading;

namespace Renderer;

public sealed class Orchestrator : IDisposable
{
    private const int SkipFrameDelayMs = 16;

    //SubClasses
    private readonly GraphicsContext Context;
    private readonly FrameManager FrameManager;
    private readonly HeapAllocator HeapAllocator;
    private readonly GeometryBuffers GeometryBuffers;
    private readonly ConstantBufferRegistry ConstantBufferRegistry;
    private readonly IConstantBufferSet[] constantBufferSets;
    private readonly IConstantUpdater[] constantUpdaters;

    private readonly SharedField.Pass fieldPass;
    private readonly SharedField.Debug fieldDebugPass;

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
            new SharedField.Buffers(Context.Device, ConstantBufferRegistry, HeapAllocator);
        var common =
            new SharedCommon.Buffers(ConstantBufferRegistry);

        constantBufferSets = [
            field,
            common
        ];

        constantUpdaters = [
            new SharedField.Controller(field),
            new SharedCommon.Controller(common)
        ];

        fieldPass = new SharedField.Pass(Context.Device, field, common);
        fieldDebugPass = new SharedField.Debug(Context.Device, field);
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
        if (WindowEnumerator.IsAnyWindowFullscreen())
        {
            FrameManager.UpdateFrameMetricOnly();
            Thread.Sleep(SkipFrameDelayMs);
            return;
        }

        Log.Debug("Render stage: begin frame");
        var currentResource = FrameManager.BeginFrame();

        Log.Debug("Render stage: update constants");
        foreach (var item in constantUpdaters)
            item.UpdateConstants(currentResource, systemSettings);

        Log.Debug("Render stage: field update");
        fieldPass.UpdateField(currentResource);

        Log.Debug("Render stage: particle constant buffers");
        foreach (var item in ParticleSystems)
            item.UpdateConstantBuffers(currentResource, systemSettings);

        Log.Debug("Render stage: particle dispatch");
        foreach (var item in ParticleSystems)
            item.Dispatch(currentResource);

        Log.Debug("Render stage: particle render");
        foreach (var item in ParticleSystems)
            item.Render(currentResource);

        
        // Log.Debug("Render stage: debug overlay");
        fieldDebugPass.Render(currentResource, systemSettings);

        Log.Debug("Render stage: end frame");
        FrameManager.EndFrame(currentResource);

    }


    public void Dispose()
    {

        FrameManager.WaitForAllFrames();

        foreach (var item in ParticleSystems)
            item.Dispose();

        fieldPass.Dispose();
        fieldDebugPass.Dispose();

        foreach (var item in constantBufferSets)
            item.Dispose();

        GeometryBuffers.Dispose();
        FrameManager.Dispose();
        HeapAllocator.Dispose();
        Context.Dispose();
    }
}
