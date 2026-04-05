using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Dxc;
using static Vortice.Direct3D12.D3D12;
sealed public class ComputePass : IDisposable
{

    //Compute
    private ID3D12RootSignature RootSignature;
    private ID3D12PipelineState ParticlePSO;
    private ID3D12PipelineState EmitterPSO;

    private ParticleBuffers ParticleBuffers;

    public ComputePass(ID3D12Device device, ParticleBuffers particleSystem, String ComputePath, String precomputePath)
    {
        ParticleBuffers = particleSystem;
        CreateComputePipeline(device, ComputePath, precomputePath);
    }

    public void Dispose()
    {
        RootSignature.Dispose();
        ParticlePSO.Dispose();
        EmitterPSO.Dispose();
    }

    private void CreateComputePipeline(ID3D12Device device, String ComputePath, String precomputePath)
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
                2,   // one UAV
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

            // u0/u1 descriptor table
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

        RootSignature = device.CreateRootSignature(0, signatureBlob);

        //
        // 3. Compile shader
        //
        ReadOnlyMemory<byte> ParticleShader = ShaderHelper.PreCompile(ComputePath, DxcShaderStage.Compute);

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
        ParticlePSO = device.CreateComputePipelineState(ParticleDesc);

        //
        // 5. Compile shader
        //
        ReadOnlyMemory<byte> EmitterShader = ShaderHelper.PreCompile(precomputePath, DxcShaderStage.Compute);

        //
        // 6. Compute PSO
        //
        var EmitterDesc = new ComputePipelineStateDescription
        {
            RootSignature = RootSignature,
            ComputeShader = EmitterShader,
            NodeMask = 0,
            CachedPSO = default,
            Flags = PipelineStateFlags.None
        };

        EmitterPSO = device.CreateComputePipelineState(EmitterDesc);
    }


    public void DispatchParticles(FrameResource currentResource, FrameManager.ConstantKey key)
    {
        var read = ParticleBuffers.ReadBufferBinding;
        var write =  ParticleBuffers.WriteBufferBinding;
        var cmd = currentResource.CommandList;
        // Transition particle buffers into correct states.
        cmd.ResourceBarrierTransition(
            read.ParticleBuffer,
            ResourceStates.UnorderedAccess,  
            ResourceStates.NonPixelShaderResource);

        cmd.SetComputeRootSignature(RootSignature);

        cmd.SetComputeRootConstantBufferView(
            0,
            currentResource.GetBuffer(key).ConstantBuffer.GPUVirtualAddress);

        // Root parameter 1 = SRV table(t0)
        cmd.SetComputeRootDescriptorTable(1, read.ParticleBufferSRVGpu);

        // Root parameter 2 = UAV table(u0)
        cmd.SetComputeRootDescriptorTable(2, write.ParticleBufferUAVGPU);

        // Sync previous frame
        cmd.ResourceBarrierUnorderedAccessView(ParticleBuffers.EmitterBuffer);

        // Emitter Update
        cmd.SetPipelineState(EmitterPSO);
        cmd.Dispatch(1, 1, 1);

        // Sync
        cmd.ResourceBarrierUnorderedAccessView(ParticleBuffers.EmitterBuffer);

        //Particle Update
        cmd.SetPipelineState(ParticlePSO);
        uint threadGroupCount = (ParticleBuffers.particleCount + 255) / 256;
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