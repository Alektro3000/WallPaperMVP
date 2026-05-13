
using Renderer.Commands;
using Renderer.Core;
using Renderer.Descriptors;
using Renderer.FrameManagement;
using Renderer.Resources;
using Settings;

public sealed class InitContext
{
    public required GraphicsContext GraphicsContext;
    public required ImmediateCommandList CommandList;
    public required ConstantBufferRegistry ConstantBufferRegistry;
    public required HeapAllocator HeapAllocator;
    public required SystemSettings SystemSettings;
}