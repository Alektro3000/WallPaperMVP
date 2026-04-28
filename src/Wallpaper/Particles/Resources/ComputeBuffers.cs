

using Particles.Resources;
using Renderer.Commands;
using Renderer.Descriptors;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Particles.Resources;

public class ComputeBuffers : IDisposable
{
    public ID3D12Resource EmitterBuffer;
    
    public readonly ID3D12Resource AliveList;
    public readonly ID3D12Resource BlockSum;
    public readonly ID3D12Resource DispatchArgs;
    
    public ComputeBuffers(ID3D12Device device, ImmediateCommandList commandList, string name, uint capacity)
    {
        EmitterBuffer = InitEmitterBuffer(device, commandList, name);
        
        AliveList = CreateActiveListBuffer(device, capacity);
        AliveList.Name = "Mouse AliveList";
        BlockSum = CreateBlockSumBuffer(device, capacity);
        BlockSum.Name = "Mouse BlockList";
        DispatchArgs = CreateDispatchArgsBuffer(device);
        DispatchArgs.Name = "Mouse Compute Dispatch Args";
    }

    public void Dispose()
    {
        AliveList?.Release();
        DispatchArgs?.Release();
        BlockSum?.Release();
        EmitterBuffer?.Dispose();
    }

    private ID3D12Resource InitEmitterBuffer(ID3D12Device device, ImmediateCommandList commandList, string name)
    {
        var buf = BufferFactory.CreateDefaultBuffer(device, [new Emitter()], commandList,
            ResourceStates.VertexAndConstantBuffer,
            ResourceFlags.AllowUnorderedAccess);
        buf.Name = name + "_EmitterBuffer";
        return buf;
    }
    
    public static uint BlockSumCapacity(uint capacity)
    {
        return (capacity+255)/256;
    }
    private ID3D12Resource CreateDispatchArgsBuffer(ID3D12Device device)
    {
        return BufferFactory.CreateDefaultBuffer<DispatchArguments>(device, 1,
            ResourceStates.IndirectArgument,
            ResourceFlags.AllowUnorderedAccess);
    }

    private ID3D12Resource CreateActiveListBuffer(ID3D12Device device, uint capacity)
    {
        return BufferFactory.CreateDefaultBuffer<uint>(device, capacity,
            ResourceStates.NonPixelShaderResource,
            ResourceFlags.AllowUnorderedAccess);
    }
    private ID3D12Resource CreateBlockSumBuffer(ID3D12Device device, uint capacity)
    {
        return BufferFactory.CreateDefaultBuffer<uint>(device, BlockSumCapacity(capacity),
            ResourceStates.NonPixelShaderResource,
            ResourceFlags.AllowUnorderedAccess);
    }
}