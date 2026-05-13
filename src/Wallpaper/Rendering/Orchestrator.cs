
using Renderer.Core;
using Serilog;
using Renderer.Descriptors;
using Particles.Settings;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Commands;
using Settings;

namespace Renderer;

public sealed class Orchestrator : IDisposable
{
    private const int SkipFrameDelayMs = 16;

    //SubClasses
    private readonly GraphicsContext Context;
    private readonly FrameManager FrameManager;
    private readonly HeapAllocator HeapAllocator;
    private readonly ParticleSystemSubsystem particleSystemSubsystem;
    private readonly ModelSubsystem ModelSubsystem;
    public Orchestrator(SystemSettings systemSettings, IntPtr hwnd, int width, int height)
    {

        Context = new GraphicsContext();
        Log.Debug("Graphic Context initialized");

        using var commandList = new ImmediateCommandList(Context);
        Log.Debug("CommandList initialized");

        HeapAllocator = new HeapAllocator(Context.Device);
        ConstantBufferRegistry ConstantBufferRegistry = new();

        InitContext initContext = new()
        {
            GraphicsContext = Context,
            CommandList = commandList,
            ConstantBufferRegistry = ConstantBufferRegistry,
            HeapAllocator = HeapAllocator,
            SystemSettings = systemSettings,
        };

        //particleSystemSubsystem = new ParticleSystemSubsystem(initContext);
        ModelSubsystem = new ModelSubsystem(initContext);

        FrameManager = new FrameManager(Context, hwnd, width, height, HeapAllocator, ConstantBufferRegistry);
        Log.Information("FrameManager Initialized {@FrameManager}", FrameManager);
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

        currentResource.Settings = systemSettings;

        ModelSubsystem?.Render(currentResource);
        particleSystemSubsystem?.Render(currentResource);

        Log.Debug("Render stage: end frame");
        FrameManager.EndFrame(currentResource);

    }


    public void Dispose()
    {

        FrameManager.WaitForAllFrames();

        ModelSubsystem.Dispose();
        particleSystemSubsystem?.Dispose();

        FrameManager.Dispose();
        HeapAllocator.Dispose();
        Context.Dispose();
    }
}
