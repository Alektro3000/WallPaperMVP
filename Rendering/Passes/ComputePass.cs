using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Dxc;
using static Vortice.Direct3D12.D3D12;
sealed class ComputePass : IDisposable
{


    //Compute
    private ID3D12RootSignature _computeRootSignature;
    private ID3D12PipelineState _computePSO;

    private ParticleBuffers _particleSystem;

    public ComputePass(ID3D12Device device, ParticleBuffers particleSystem)
    {
        _particleSystem = particleSystem;
        CreateComputePipeline(device);
    }

    public void Dispose()
    {
        _computeRootSignature.Dispose();
        _computePSO.Dispose();
    }

    private void CreateComputePipeline(ID3D12Device device)
    {
        //
        // 2. Root signature
        //
        var ranges = new[]
        {
            new DescriptorRange1(
                DescriptorRangeType.ShaderResourceView,
                1,   // one SRV
                0,   // t0 
                0,
                (uint)DescriptorRangeFlags.None,
                0),

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

            // t0 descriptor table
            new RootParameter1(new RootDescriptorTable1(ranges[0]), ShaderVisibility.All),

            // u0 descriptor table
            new RootParameter1(new RootDescriptorTable1(ranges[1]), ShaderVisibility.All)
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

        _computeRootSignature = device.CreateRootSignature(0, signatureBlob);

        //
        // 3. Compile shader
        //
        ReadOnlyMemory<byte> computeShader = ShaderHelper.PreCompile("compute.hlsl", DxcShaderStage.Compute);

        //
        // 4. Compute PSO
        //
        var psoDesc = new ComputePipelineStateDescription
        {
            RootSignature = _computeRootSignature,
            ComputeShader = computeShader,
            NodeMask = 0,
            CachedPSO = default,
            Flags = PipelineStateFlags.None
        };

        _computePSO = device.CreateComputePipelineState(psoDesc);
    }


    public void DispatchParticles(
    FrameResource frameResource)
    {
        var read = _particleSystem.ReadBufferBinding;
        var write =  _particleSystem.WriteBufferBinding;
        var cmd = frameResource.CommandList;
        // Transition particle buffers into correct states.
        cmd.ResourceBarrierTransition(
            read.ParticleBuffer,
            ResourceStates.UnorderedAccess,  
            ResourceStates.NonPixelShaderResource);

        cmd.SetComputeRootSignature(_computeRootSignature);
        cmd.SetPipelineState(_computePSO);

        cmd.SetComputeRootConstantBufferView(
            0,
            frameResource.ConstantBuffer.GPUVirtualAddress);

        // Root parameter 1 = SRV table(t0)
        cmd.SetComputeRootDescriptorTable(1, read.ParticleBufferSRVGpu);

        // Root parameter 2 = UAV table(u0)
        cmd.SetComputeRootDescriptorTable(2, write.ParticleBufferUAVGPU);

        uint threadGroupCount = (_particleSystem._particleCount + 255) / 256;
        cmd.Dispatch(threadGroupCount, 1, 1);

        // Ensure UAV writes are visible before later use.
        cmd.ResourceBarrier(new ResourceBarrier(new ResourceUnorderedAccessViewBarrier(write.ParticleBuffer)));

        // Example: if you will render from _particleBufferWrite afterward.
        cmd.ResourceBarrierTransition(
            write.ParticleBuffer,
            ResourceStates.UnorderedAccess,
            ResourceStates.NonPixelShaderResource);

    }

}