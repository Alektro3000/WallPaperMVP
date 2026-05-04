using Particles.Core;
using Particles.Resources;
using Particles.Systems.Mouse;
using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Shaders;
using Vortice.Direct3D12;
using static Vortice.Direct3D12.D3D12;

namespace Particles.Passes;

public class Compute : IDisposable
{

    //Compute
    private ID3D12RootSignature RootSignature;

    private ID3D12CommandSignature DispatchCommandSignature;

    private ID3D12PipelineState ParticlePSO;

    private ID3D12PipelineState EmitterPSO;
    private ID3D12PipelineState MarkAlivePSO;
    private ID3D12PipelineState CopyPSO;
    private ID3D12PipelineState DrawCountPSO;
    private ID3D12PipelineState DrawCountNoCompactPSO;

    private ID3D12PipelineState PrefixLocalPSO;
    private ID3D12PipelineState PrefixGlobalPSO;
    private ID3D12PipelineState PrefixAddOffsetPSO;
    private ParticleComputeBindings BufferTable;

    public Compute(ID3D12Device device, ParticleComputeBindings bufferTable, String computePath, String emitterPath, String drawCountPath)
    {
        BufferTable = bufferTable;

        RootSignature = CreateRootSignature(device);

        ParticlePSO = ShaderLibrary.CreatePSO(device, RootSignature, computePath);
        EmitterPSO = ShaderLibrary.CreatePSO(device, RootSignature, emitterPath);
        
        MarkAlivePSO = ShaderLibrary.CreatePSO(device, RootSignature, "shared/alive.hlsl");
        CopyPSO = ShaderLibrary.CreatePSO(device, RootSignature, "shared/copy.hlsl");

        DrawCountPSO = ShaderLibrary.CreatePSO(device, RootSignature, drawCountPath);
        DrawCountNoCompactPSO = ShaderLibrary.CreatePSO(device, RootSignature, "shared/draw_count_no_compact.hlsl");


        PrefixLocalPSO = ShaderLibrary.CreatePSO(device, RootSignature, "shared/prefix_local.hlsl");
        PrefixGlobalPSO = ShaderLibrary.CreatePSO(device, RootSignature, "shared/prefix_block_sums.hlsl");
        PrefixAddOffsetPSO = ShaderLibrary.CreatePSO(device, RootSignature, "shared/prefix_add_offset.hlsl");


        var commandSigDesc = new CommandSignatureDescription([new IndirectArgumentDescription
            {
                Type = IndirectArgumentType.Dispatch
            }])
        {
            ByteStride = 12,
        };

        DispatchCommandSignature = device.CreateCommandSignature<ID3D12CommandSignature>(commandSigDesc, null);
    
    }

    public void Dispose()
    {
        MarkAlivePSO.Dispose();
        CopyPSO.Dispose();
        DrawCountPSO.Dispose();
        PrefixLocalPSO.Dispose();
        PrefixGlobalPSO.Dispose();
        PrefixAddOffsetPSO.Dispose();
        RootSignature.Dispose();
        ParticlePSO.Dispose();
        EmitterPSO.Dispose();
        DispatchCommandSignature.Dispose();
    }

    protected virtual ID3D12RootSignature CreateRootSignature(ID3D12Device device)
    {
        var rootParams = new[]
        {
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(0, 0), ShaderVisibility.All), // b0 particle constants

            new RootParameter1(new RootDescriptorTable1(
                new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 0)), ShaderVisibility.All), // t0 PrevParticles

            new RootParameter1(new RootDescriptorTable1(
                new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, 1, 0)), ShaderVisibility.All), // u0 //CurrentParticles

            new RootParameter1(new RootDescriptorTable1(
                new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, 6, 1)), ShaderVisibility.All), // u1..u4

            new RootParameter1(new RootDescriptorTable1(
                new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 1)), ShaderVisibility.All), // t1 field SRV

            new RootParameter1(new RootDescriptorTable1(
                new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 3, 2)), ShaderVisibility.All), // t2..t4 helper SRVs

            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(1, 0), ShaderVisibility.All), // b1 common
        };

        var staticSampler = new StaticSamplerDescription(
            ShaderVisibility.All,
            0, // s0
            0  // space0
        )
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
    protected void TransitToUnordered(ID3D12GraphicsCommandList cmd, ID3D12Resource resource)
    {
        cmd.ResourceBarrierTransition(
            resource,
            ResourceStates.NonPixelShaderResource,
            ResourceStates.UnorderedAccess);
    }
    protected void TransitToNonPixel(ID3D12GraphicsCommandList cmd, ID3D12Resource resource)
    {
        cmd.ResourceBarrierTransition(
            resource,
            ResourceStates.UnorderedAccess,
            ResourceStates.NonPixelShaderResource);
    }


    private void ExecuteIndirect(ID3D12GraphicsCommandList cmd)
    {
        cmd.ExecuteIndirect(DispatchCommandSignature, 1, BufferTable.ComputeBuffers.DispatchArgs, 0, null, 0);
    }
    
    public void DispatchParticles(FrameResource currentResource, IConstantBufferKey key, bool shouldCompact)
    {
        var compact = BufferTable.ParticleBuffers.WriteBufferBinding;
        var sparse = BufferTable.ParticleBuffers.ReadBufferBinding;
        var cmd = currentResource.CommandList;

        cmd.SetComputeRootSignature(RootSignature);

        cmd.SetComputeRootConstantBufferView(
            0,
            currentResource.GetGPUVirtualAddress(key));

        // Root parameter 1 = SRV table(t0)
        cmd.SetComputeRootDescriptorTable(1, compact.ParticleBufferSRV.Gpu);

        // Root parameter 2 = UAV table(u0)
        cmd.SetComputeRootDescriptorTable(2, sparse.ParticleBufferUAV.Gpu);

        // Root parameter 3 = UAV table(u1-u4)
        cmd.SetComputeRootDescriptorTable(3, BufferTable.UavsStart);

        // Root parameter 4 = SRV table(t1)
        cmd.SetComputeRootDescriptorTable(4, BufferTable.FieldBuffers.SRVFieldDescriptor);

        // Root parameter 5 = SRV table(t2-t4)
        cmd.SetComputeRootDescriptorTable(5, BufferTable.SrvsStart);

        // Root parameter 6 = CBV 
        cmd.SetComputeRootConstantBufferView(6,
                currentResource.GetGPUVirtualAddress(BufferTable.CommonBuffers.commonKey));


        // Sync
        cmd.ResourceBarrierUnorderedAccessView(BufferTable.ComputeBuffers.EmitterBuffer);

        // Emitter Update
        cmd.ResourceBarrierTransition(
            BufferTable.ComputeBuffers.DispatchArgs,
            ResourceStates.IndirectArgument,
            ResourceStates.UnorderedAccess);

        cmd.SetPipelineState(EmitterPSO);
        cmd.Dispatch(1, 1, 1);

        cmd.ResourceBarrierTransition(
            BufferTable.ComputeBuffers.DispatchArgs,
            ResourceStates.UnorderedAccess,
            ResourceStates.IndirectArgument);

        cmd.ResourceBarrierUnorderedAccessView(BufferTable.ComputeBuffers.EmitterBuffer);

        //Particle Update
        TransitToUnordered(cmd, sparse.ParticleBuffer);
        cmd.SetPipelineState(ParticlePSO);
        ExecuteIndirect(cmd);
        TransitToNonPixel(cmd, sparse.ParticleBuffer);

        if (shouldCompact)
        {
            Compact(cmd, compact, sparse);
        }
        else
        {
            NoCompact(cmd, compact, sparse);
            BufferTable.ParticleBuffers.SwapBuffers();
        }
    }

    public void Compact(ID3D12GraphicsCommandList cmd, ParticleBuffers.ParticleBufferBinding compact, ParticleBuffers.ParticleBufferBinding sparse)
    {
        // Root parameter 1 = SRV table(t0)
        cmd.SetComputeRootDescriptorTable(1, sparse.ParticleBufferSRV.Gpu);
        // Root parameter 2 = UAV table(u0)
        cmd.SetComputeRootDescriptorTable(2, compact.ParticleBufferUAV.Gpu);

        // Mark Alive
        TransitToUnordered(cmd, BufferTable.ComputeBuffers.AliveList);
        cmd.SetPipelineState(MarkAlivePSO);
        ExecuteIndirect(cmd);
        cmd.ResourceBarrierUnorderedAccessView(BufferTable.ComputeBuffers.AliveList);

        // Prefix
        TransitToUnordered(cmd, BufferTable.ComputeBuffers.BlockSum);
        cmd.SetPipelineState(PrefixLocalPSO);
        ExecuteIndirect(cmd);

        cmd.ResourceBarrierUnorderedAccessView(BufferTable.ComputeBuffers.AliveList);
        cmd.ResourceBarrierUnorderedAccessView(BufferTable.ComputeBuffers.BlockSum);

        cmd.SetPipelineState(PrefixGlobalPSO);
        cmd.Dispatch(1, 1, 1);

        // global prefix записал BlockSum offsets
        TransitToNonPixel(cmd, BufferTable.ComputeBuffers.BlockSum);

        cmd.SetPipelineState(PrefixAddOffsetPSO);
        ExecuteIndirect(cmd);

        cmd.ResourceBarrierUnorderedAccessView(BufferTable.ComputeBuffers.AliveList);
        TransitToNonPixel(cmd, BufferTable.ComputeBuffers.AliveList);


        // Copy
        TransitToUnordered(cmd, compact.ParticleBuffer);
        cmd.SetPipelineState(CopyPSO);
        ExecuteIndirect(cmd);
        TransitToNonPixel(cmd, compact.ParticleBuffer);

        //Present
        cmd.ResourceBarrierUnorderedAccessView(BufferTable.ComputeBuffers.EmitterBuffer);
        cmd.ResourceBarrierTransition(
            BufferTable.ParticleBuffers.DrawArgs,
            ResourceStates.IndirectArgument,
            ResourceStates.UnorderedAccess);

        cmd.SetPipelineState(DrawCountPSO);
        cmd.Dispatch(1, 1, 1);

        cmd.ResourceBarrierTransition(
            BufferTable.ParticleBuffers.DrawArgs,
            ResourceStates.UnorderedAccess,
            ResourceStates.IndirectArgument);
    }

    public void NoCompact(ID3D12GraphicsCommandList cmd, ParticleBuffers.ParticleBufferBinding compact, ParticleBuffers.ParticleBufferBinding sparse)
    {
        //Present
        cmd.ResourceBarrierUnorderedAccessView(BufferTable.ComputeBuffers.EmitterBuffer);
        cmd.ResourceBarrierTransition(
            BufferTable.ParticleBuffers.DrawArgs,
            ResourceStates.IndirectArgument,
            ResourceStates.UnorderedAccess);

        cmd.SetPipelineState(DrawCountNoCompactPSO);
        cmd.Dispatch(1, 1, 1);

        cmd.ResourceBarrierTransition(
            BufferTable.ParticleBuffers.DrawArgs,
            ResourceStates.UnorderedAccess,
            ResourceStates.IndirectArgument);
    }

    internal void DispatchParticles(FrameResource currentResource, object constantKey, bool v)
    {
        throw new NotImplementedException();
    }
}