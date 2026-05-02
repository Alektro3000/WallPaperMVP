using Particles.Resources;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Shaders;
using Vortice.Direct3D12;
using static Vortice.Direct3D12.D3D12;

namespace Particles.Systems.Fluid;

public sealed class FluidCompute : IDisposable
{
    public const uint MaxGridCells = 1024;

    private readonly ID3D12RootSignature rootSignature;
    private readonly ID3D12PipelineState emitterPso;
    private readonly ID3D12PipelineState updatePso;
    private readonly ID3D12PipelineState sortPso;
    private readonly ID3D12PipelineState clearGridPso;
    private readonly ID3D12PipelineState rangesPso;
    private readonly ID3D12PipelineState drawCountPso;
    private readonly ID3D12CommandSignature dispatchCommandSignature;
    private readonly ParticleComputeBindings bindings;
    private readonly FluidBuffers fluidBuffers;
    private readonly GpuDescriptorHandle fluidUavs;
    private readonly GpuDescriptorHandle fluidSrvs;
    private readonly uint capacity;
    
    private void TransitToUnordered(ID3D12GraphicsCommandList cmd, ID3D12Resource resource)
    {
        cmd.ResourceBarrierTransition(
            resource,
            ResourceStates.NonPixelShaderResource,
            ResourceStates.UnorderedAccess);
    }
    private void TransitToNonPixel(ID3D12GraphicsCommandList cmd, ID3D12Resource resource)
    {
        cmd.ResourceBarrierTransition(
            resource,
            ResourceStates.UnorderedAccess,
            ResourceStates.NonPixelShaderResource);
    }

    public FluidCompute(ID3D12Device device, ParticleComputeBindings bindings, uint capacity, FluidBuffers fluidBuffers, GpuDescriptorHandle fluidUavs, GpuDescriptorHandle fluidSrvs)
    {
        this.bindings = bindings;
        this.capacity = capacity;
        this.fluidBuffers = fluidBuffers;
        this.fluidUavs = fluidUavs;
        this.fluidSrvs = fluidSrvs;

        rootSignature = CreateFluidRootSignature(device);
        emitterPso = ShaderLibrary.CreatePSO(device, rootSignature, "fluid\\emitter.hlsl");
        updatePso = ShaderLibrary.CreatePSO(device, rootSignature, "fluid\\compute.hlsl");
        sortPso = ShaderLibrary.CreatePSO(device, rootSignature, "fluid\\sort_hash.hlsl");
        clearGridPso = ShaderLibrary.CreatePSO(device, rootSignature, "fluid\\clear_grid.hlsl");
        rangesPso = ShaderLibrary.CreatePSO(device, rootSignature, "fluid\\build_ranges.hlsl");
        drawCountPso = ShaderLibrary.CreatePSO(device, rootSignature, "fluid\\draw_count.hlsl");

        var commandSigDesc = new CommandSignatureDescription([new IndirectArgumentDescription { Type = IndirectArgumentType.Dispatch }])
        {
            ByteStride = 12,
        };
        dispatchCommandSignature = device.CreateCommandSignature<ID3D12CommandSignature>(commandSigDesc, null);
    }

    public void DispatchParticles(FrameResource currentResource, ConstantBufferKey key, bool shouldCompact)
    {
        var read = bindings.ParticleBuffers.WriteBufferBinding;
        var write = bindings.ParticleBuffers.ReadBufferBinding;
        var cmd = currentResource.CommandList;

        cmd.SetComputeRootSignature(rootSignature);
        cmd.SetComputeRootConstantBufferView(0, currentResource.GetGPUVirtualAddress(key));
        cmd.SetComputeRootDescriptorTable(1, read.ParticleBufferSRV.Gpu);
        cmd.SetComputeRootDescriptorTable(2, write.ParticleBufferUAV.Gpu);
        cmd.SetComputeRootDescriptorTable(3, bindings.UavsStart);
        cmd.SetComputeRootDescriptorTable(4, bindings.FieldBuffers.SRVFieldDescriptor);
        cmd.SetComputeRootDescriptorTable(5, bindings.SrvsStart);
        cmd.SetComputeRootConstantBufferView(6, currentResource.GetGPUVirtualAddress(bindings.CommonBuffers.commonKey));
        cmd.SetComputeRootDescriptorTable(7, fluidUavs);
        cmd.SetComputeRootDescriptorTable(8, fluidSrvs);

        cmd.ResourceBarrierTransition(bindings.ComputeBuffers.DispatchArgs, ResourceStates.IndirectArgument, ResourceStates.UnorderedAccess);
        cmd.SetPipelineState(emitterPso);
        cmd.Dispatch(1, 1, 1);
        cmd.ResourceBarrierUnorderedAccessView(bindings.ComputeBuffers.EmitterBuffer);
        cmd.ResourceBarrierTransition(bindings.ComputeBuffers.DispatchArgs, ResourceStates.UnorderedAccess, ResourceStates.IndirectArgument);

        TransitToUnordered(cmd, fluidBuffers.HashEntries);
        cmd.SetComputeRoot32BitConstants<uint>(9, [0u, 0u, capacity, 0u], 0);
        cmd.SetPipelineState(sortPso);
        DispatchIndirect(cmd);
        cmd.ResourceBarrierUnorderedAccessView(fluidBuffers.HashEntries);

        uint sortCount = NextPowerOfTwo(capacity);
        for (uint k = 2; k <= sortCount; k <<= 1)
        {
            for (uint j = k >> 1; j > 0; j >>= 1)
            {
                cmd.SetComputeRoot32BitConstants<uint>(9, [j, k, capacity, 0u], 0);
                cmd.SetPipelineState(sortPso);
                cmd.Dispatch((capacity + 255) / 256, 1, 1);
                cmd.ResourceBarrierUnorderedAccessView(fluidBuffers.HashEntries);
            }
        }
        TransitToNonPixel(cmd, fluidBuffers.HashEntries);

        TransitToUnordered(cmd, fluidBuffers.CellRanges);
        cmd.SetPipelineState(clearGridPso);
        cmd.Dispatch((MaxGridCells + 255) / 256, 1, 1);
        cmd.ResourceBarrierUnorderedAccessView(fluidBuffers.CellRanges);

        cmd.SetPipelineState(rangesPso);
        DispatchIndirect(cmd);
        TransitToNonPixel(cmd, fluidBuffers.CellRanges);

        TransitToUnordered(cmd, write.ParticleBuffer);
        cmd.SetPipelineState(updatePso);
        DispatchIndirect(cmd);
        TransitToNonPixel(cmd, write.ParticleBuffer);

        cmd.ResourceBarrierTransition(bindings.ParticleBuffers.DrawArgs, ResourceStates.IndirectArgument, ResourceStates.UnorderedAccess);
        cmd.SetPipelineState(drawCountPso);
        cmd.Dispatch(1, 1, 1);
        cmd.ResourceBarrierTransition(bindings.ParticleBuffers.DrawArgs, ResourceStates.UnorderedAccess, ResourceStates.IndirectArgument);

        bindings.ParticleBuffers.SwapBuffers();
    }

    private static uint NextPowerOfTwo(uint value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    private void DispatchIndirect(ID3D12GraphicsCommandList cmd)
        => cmd.ExecuteIndirect(dispatchCommandSignature, 1, bindings.ComputeBuffers.DispatchArgs, 0, null, 0);

    private static ID3D12RootSignature CreateFluidRootSignature(ID3D12Device device)
    {
        var rootParams = new[]
        {
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(0, 0), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 0)), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, 1, 0)), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, 5, 1)), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 1)), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 2, 2)), ShaderVisibility.All),
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(1, 0), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, 2, 6)), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 2, 4)), ShaderVisibility.All),
            new RootParameter1(new RootConstants(2, 0, 4), ShaderVisibility.All),
        };

        var staticSampler = new StaticSamplerDescription(ShaderVisibility.All, 0, 0)
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            ComparisonFunction = ComparisonFunction.Never,
            MaxLOD = float.MaxValue
        };

        return ShaderLibrary.CreateRootSignature(device, rootParams, [staticSampler]);
    }

    public void Dispose()
    {
        drawCountPso.Dispose();
        rangesPso.Dispose();
        clearGridPso.Dispose();
        sortPso.Dispose();
        updatePso.Dispose();
        emitterPso.Dispose();
        dispatchCommandSignature.Dispose();
        rootSignature.Dispose();
        fluidBuffers.Dispose();
    }
}
