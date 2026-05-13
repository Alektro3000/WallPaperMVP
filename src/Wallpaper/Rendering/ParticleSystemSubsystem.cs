using Renderer.Core;
using Serilog;
using SharedField = Particles.Shared.Field;
using SharedCommon = Particles.Shared.Global;
using Renderer.Descriptors;
using Particles.Settings;
using Particles.Core;
using Renderer.FrameManagement;
using Particles.Resources;
using Renderer.Commands;
using Particles.Shared;
using System.Threading;


class ParticleSystemSubsystem : IDisposable
{
    private readonly GeometryBuffers GeometryBuffers;
    private readonly IConstantBufferSet[] constantBufferSets;
    private readonly IConstantUpdater[] constantUpdaters;

    private readonly SharedField.Pass fieldPass;
    private readonly SharedField.Debug fieldDebugPass;

    public readonly IParticleSystem[] ParticleSystems;

    public ParticleSystemSubsystem(InitContext initContext)
    {
        var Context = initContext.GraphicsContext;
        GeometryBuffers = new GeometryBuffers(Context.Device, initContext.CommandList, initContext.HeapAllocator);


        var field =
            new SharedField.Buffers(Context.Device, initContext.ConstantBufferRegistry, initContext.HeapAllocator);
        var common =
            new SharedCommon.Buffers(initContext.ConstantBufferRegistry);

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
            CommandList = initContext.CommandList,
            GeometryBuffers = GeometryBuffers,
            HeapAllocator = initContext.HeapAllocator,
            CommonBuffers = common,
            FieldBuffers = field,
            Registry = initContext.ConstantBufferRegistry
        };

        ParticleSystems = ParticleSystemReflection.CreateParticleSystems(initContext.SystemSettings, context).ToArray();
    }

    public void Render(FrameResource currentResource)
    {
        Log.Debug("Render stage: update constants");
        foreach (var item in constantUpdaters)
            item.UpdateConstants(currentResource);

        Log.Debug("Render stage: field update");
        fieldPass.UpdateField(currentResource);

        Log.Debug("Render stage: particle constant buffers");
        foreach (var item in ParticleSystems)
            item.UpdateConstantBuffers(currentResource);

        Log.Debug("Render stage: particle dispatch");
        foreach (var item in ParticleSystems)
            item.Dispatch(currentResource);

        Log.Debug("Render stage: particle render");
        foreach (var item in ParticleSystems)
            item.Render(currentResource);

        
        // Log.Debug("Render stage: debug overlay");
        fieldDebugPass.Render(currentResource, currentResource.Settings);
    }
    public void Dispose()
    {
        foreach (var item in ParticleSystems)
            item.Dispose();

        fieldPass.Dispose();
        fieldDebugPass.Dispose();

        foreach (var item in constantBufferSets)
            item.Dispose();

        GeometryBuffers.Dispose();
    }
}