using System.Xml.Serialization;
using Microsoft.VisualBasic.Devices;
using Vortice.Direct3D12;

namespace ParticleSystems.Mouse;

[Shader("mouse\\compute.hlsl", "cs")]
[Shader("mouse\\emitter.hlsl", "cs")]
[Shader("mouse\\alive.hlsl", "cs")]
[Shader("mouse\\prefix_local.hlsl", "cs")]
[Shader("mouse\\prefix_block_sums.hlsl", "cs")]
[Shader("mouse\\prefix_add_offset.hlsl", "cs")]
[Shader("mouse\\copy.hlsl", "cs")]
[Shader("mouse\\draw_count.hlsl", "cs")]
[Shader("mouse\\draw_count_no_compact.hlsl", "cs")]
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

    private ParticleBuffers ParticleBuffers;
    private Buffer MouseBuffer;
    private CommonBuffers CommonBuffers;
    private FieldBuffers FieldBuffers;

    public Compute(ID3D12Device device, ParticleBuffers particleSystem, Buffer mouseBuffer, CommonBuffers commonBuffers, FieldBuffers fieldBuffers)
    {
        ParticleBuffers = particleSystem;
        FieldBuffers = fieldBuffers;
        MouseBuffer = mouseBuffer;
        CommonBuffers = commonBuffers;
        RootSignature = CreateRootSignature(device);
        ParticlePSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/compute.hlsl");
        EmitterPSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/emitter.hlsl");
        MarkAlivePSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/alive.hlsl");
        CopyPSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/copy.hlsl");

        DrawCountPSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/draw_count.hlsl");
        DrawCountNoCompactPSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/draw_count_no_compact.hlsl");


        PrefixLocalPSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/prefix_local.hlsl");
        PrefixGlobalPSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/prefix_block_sums.hlsl");
        PrefixAddOffsetPSO = ShaderHelper.CreatePSO(device, RootSignature, "mouse/prefix_add_offset.hlsl");


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

        return ShaderHelper.CreateRootSignature(device, rootParams, [staticSampler]);
    }
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
    private void ExecuteIndirect(ID3D12GraphicsCommandList cmd)
    {
        cmd.ExecuteIndirect(DispatchCommandSignature, 1, MouseBuffer.DispatchArgs, 0, null, 0);
    }
    public void DispatchParticles(FrameResource currentResource, FrameManager.ConstantKey key, bool isCompactPass)
    {
        var compact = ParticleBuffers.WriteBufferBinding;
        var sparse = ParticleBuffers.ReadBufferBinding;
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
        cmd.SetComputeRootDescriptorTable(3, MouseBuffer.UavsStart);

        // Root parameter 4 = SRV table(t1)
        cmd.SetComputeRootDescriptorTable(4, FieldBuffers.SRVFieldDescriptor);

        // Root parameter 5 = SRV table(t2-t4)
        cmd.SetComputeRootDescriptorTable(5, MouseBuffer.SrvsStart);

        // Root parameter 6 = CBV 
        cmd.SetComputeRootConstantBufferView(6,
                currentResource.GetGPUVirtualAddress(CommonBuffers.commonKey));


        // Sync
        cmd.ResourceBarrierUnorderedAccessView(ParticleBuffers.EmitterBuffer);

        // Emitter Update
        cmd.ResourceBarrierTransition(
            MouseBuffer.DispatchArgs,
            ResourceStates.IndirectArgument,
            ResourceStates.UnorderedAccess);

        cmd.SetPipelineState(EmitterPSO);
        cmd.Dispatch(1, 1, 1);

        cmd.ResourceBarrierTransition(
            MouseBuffer.DispatchArgs,
            ResourceStates.UnorderedAccess,
            ResourceStates.IndirectArgument);

        cmd.ResourceBarrierUnorderedAccessView(ParticleBuffers.EmitterBuffer);

        //Particle Update
        TransitToUnordered(cmd, sparse.ParticleBuffer);
        cmd.SetPipelineState(ParticlePSO);
        ExecuteIndirect(cmd);
        TransitToNonPixel(cmd, sparse.ParticleBuffer);

        if (isCompactPass)
        {
            Compact(cmd, compact, sparse);
        }
        else
        {
            NoCompact(cmd, compact, sparse);
        }
    }

    public void Compact(ID3D12GraphicsCommandList cmd, ParticleBuffers.ParticleBufferBinding compact, ParticleBuffers.ParticleBufferBinding sparse)
    {
        // Root parameter 1 = SRV table(t0)
        cmd.SetComputeRootDescriptorTable(1, sparse.ParticleBufferSRV.Gpu);
        // Root parameter 2 = UAV table(u0)
        cmd.SetComputeRootDescriptorTable(2, compact.ParticleBufferUAV.Gpu);

        // Mark Alive
        TransitToUnordered(cmd, MouseBuffer.AliveList);
        cmd.SetPipelineState(MarkAlivePSO);
        ExecuteIndirect(cmd);
        cmd.ResourceBarrierUnorderedAccessView(MouseBuffer.AliveList);

        // Prefix
        TransitToUnordered(cmd, MouseBuffer.BlockSum);
        cmd.SetPipelineState(PrefixLocalPSO);
        ExecuteIndirect(cmd);

        cmd.ResourceBarrierUnorderedAccessView(MouseBuffer.AliveList);
        cmd.ResourceBarrierUnorderedAccessView(MouseBuffer.BlockSum);

        cmd.SetPipelineState(PrefixGlobalPSO);
        cmd.Dispatch(1, 1, 1);

        // global prefix записал BlockSum offsets
        TransitToNonPixel(cmd, MouseBuffer.BlockSum);

        cmd.SetPipelineState(PrefixAddOffsetPSO);
        ExecuteIndirect(cmd);

        cmd.ResourceBarrierUnorderedAccessView(MouseBuffer.AliveList);
        TransitToNonPixel(cmd, MouseBuffer.AliveList);


        // Copy
        TransitToUnordered(cmd, compact.ParticleBuffer);
        cmd.SetPipelineState(CopyPSO);
        ExecuteIndirect(cmd);
        TransitToNonPixel(cmd, compact.ParticleBuffer);

        //Present
        cmd.ResourceBarrierUnorderedAccessView(ParticleBuffers.EmitterBuffer);
        cmd.ResourceBarrierTransition(
            ParticleBuffers.DrawArgs,
            ResourceStates.IndirectArgument,
            ResourceStates.UnorderedAccess);

        cmd.SetPipelineState(DrawCountPSO);
        cmd.Dispatch(1, 1, 1);

        cmd.ResourceBarrierTransition(
            ParticleBuffers.DrawArgs,
            ResourceStates.UnorderedAccess,
            ResourceStates.IndirectArgument);
    }

    public void NoCompact(ID3D12GraphicsCommandList cmd, ParticleBuffers.ParticleBufferBinding compact, ParticleBuffers.ParticleBufferBinding sparse)
    {
        //Present
        cmd.ResourceBarrierUnorderedAccessView(ParticleBuffers.EmitterBuffer);
        cmd.ResourceBarrierTransition(
            ParticleBuffers.DrawArgs,
            ResourceStates.IndirectArgument,
            ResourceStates.UnorderedAccess);

        cmd.SetPipelineState(DrawCountNoCompactPSO);
        cmd.Dispatch(1, 1, 1);

        cmd.ResourceBarrierTransition(
            ParticleBuffers.DrawArgs,
            ResourceStates.UnorderedAccess,
            ResourceStates.IndirectArgument);
    }
}