using Vortice.Direct3D12;
using Vortice.Dxc;
using static Vortice.Direct3D12.D3D12;
sealed public class FieldPass : IDisposable
{

    //Compute
    private ID3D12RootSignature RootSignature;
    private ID3D12PipelineState FieldPSO;
    private FieldBuffers FieldBuffers;
    private CommonBuffers CommonBuffers;

    public FieldPass(ID3D12Device device, FieldBuffers fieldBuffers, CommonBuffers commonBuffers, String FieldPath)
    {
        FieldBuffers = fieldBuffers;
        CommonBuffers = commonBuffers;
        CreateComputePipeline(device, FieldPath);
    }

    public void Dispose()
    {
        RootSignature.Dispose();
    }

    private void CreateComputePipeline(ID3D12Device device, String FieldPath)
    {
        //
        // 2. Root signature
        //
        var ranges = new[]
        {
            new DescriptorRange1(
                DescriptorRangeType.UnorderedAccessView,
                1,   // one UAV
                0,   // u0
                0,
                (uint)DescriptorRangeFlags.None,
                0)
        };

        var rootParams = new[]
        {
            // b0 as root CBV
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(0, 0), ShaderVisibility.All),
            
            // u0 as descriptor table texture UAV
            new RootParameter1(new RootDescriptorTable1(ranges[0]), ShaderVisibility.All),
            
            // b1 as root CBV
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(1, 0), ShaderVisibility.All),

        };

        var rootSigDesc = new VersionedRootSignatureDescription(
            new RootSignatureDescription1(
                RootSignatureFlags.None,
                rootParams,
                null));

        Vortice.Direct3D.Blob signatureBlob;
        string error = D3D12SerializeVersionedRootSignature(rootSigDesc, out signatureBlob);

        if (signatureBlob == null)
        {
            throw new InvalidOperationException(error);
        }

        RootSignature = device.CreateRootSignature(0, signatureBlob);

        //
        // 3. Compile shader
        //
        ReadOnlyMemory<byte> ParticleShader = ShaderHelper.PreCompile(FieldPath, DxcShaderStage.Compute);

        //
        // 4. Compute PSO
        //
        var ParticleDesc = new ComputePipelineStateDescription
        {
            RootSignature = RootSignature,
            ComputeShader = ParticleShader,
            NodeMask = 0,
            CachedPSO = default,
            Flags = PipelineStateFlags.None
        };

        FieldPSO = device.CreateComputePipelineState(ParticleDesc);
    }

    public void UpdateField(FrameResource currentResource)
    {
        var cmd = currentResource.CommandList;

        // Transition particle buffers into correct states.            
        cmd.ResourceBarrierTransition(
            FieldBuffers.FieldResource,
            ResourceStates.AllShaderResource,
            ResourceStates.UnorderedAccess);

        cmd.SetPipelineState(FieldPSO);
        cmd.SetComputeRootSignature(RootSignature);

        cmd.SetComputeRootConstantBufferView(
            0,
            currentResource.GetGPUVirtualAddress(FieldBuffers.fieldKey));

        // Root parameter 1 = UAV table(t0)
        cmd.SetComputeRootDescriptorTable(1, FieldBuffers.UAVFieldDescriptor);

        
        cmd.SetComputeRootConstantBufferView(
            2,
            currentResource.GetGPUVirtualAddress(CommonBuffers.commonKey));

        cmd.Dispatch((FieldBuffers.width + 7) / 8, (FieldBuffers.height + 7) / 8, 1);

        // Ensure UAV writes are visible before later use in vertex shader
        cmd.ResourceBarrierTransition(
            FieldBuffers.FieldResource,
            ResourceStates.UnorderedAccess,
            ResourceStates.AllShaderResource);
    }

}