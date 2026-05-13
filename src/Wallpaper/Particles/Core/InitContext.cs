
using Vortice.Direct3D12;
using SharedField = Particles.Shared.Field;
using SharedCommon = Particles.Shared.Global;
using Renderer.Descriptors;
using Renderer.Resources;
using Renderer.FrameManagement;
using Renderer.Commands;
using Particles.Resources;

namespace Particles.Core;

public sealed record ParticleSystemInitContext
{
    public required ID3D12Device Device;
    public required ImmediateCommandList CommandList;
    public required GeometryBuffers GeometryBuffers;
    public required HeapAllocator HeapAllocator;
    public required SharedCommon.Buffers CommonBuffers;
    public required SharedField.Buffers FieldBuffers;
    public required ConstantBufferRegistry Registry;
}