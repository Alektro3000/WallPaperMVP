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
    private CommonBuffers CommonBuffers;
    private FieldBuffers FieldBuffers;

    public ComputePass(ID3D12Device device, ParticleBuffers particleSystem, CommonBuffers commonBuffers, FieldBuffers fieldBuffers, String ComputePath, String precomputePath)
    {
        ParticleBuffers = particleSystem;
        FieldBuffers = fieldBuffers;
        CommonBuffers = commonBuffers;
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

        //
        // 2. Root signature
        //
        var rootParams = new[]
        {
            // b0 as root CBV
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(0, 0), ShaderVisibility.All),

            // t0 descriptor table - ping pong read
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(
                DescriptorRangeType.ShaderResourceView,
                1,   // 
                0)), ShaderVisibility.All),

            // u0/u1 descriptor table - ping pong write + emitter buffer
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(
                DescriptorRangeType.UnorderedAccessView,
                2,
                0)), ShaderVisibility.All),

            // t1 descriptor table - force field
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(
                DescriptorRangeType.ShaderResourceView,
                1,   // 
                1)), ShaderVisibility.All),

            // b1 as root CBV
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(1,0), ShaderVisibility.All),
        };

        var rootSigDesc = new VersionedRootSignatureDescription(
            new RootSignatureDescription1(
                RootSignatureFlags.None,
                rootParams,
                [staticSampler]));

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
        var write = ParticleBuffers.WriteBufferBinding;
        var cmd = currentResource.CommandList;

        // Transition particle buffers into correct states.            
        cmd.ResourceBarrierTransition(
            write.ParticleBuffer,
            ResourceStates.NonPixelShaderResource,
            ResourceStates.UnorderedAccess);

        cmd.SetComputeRootSignature(RootSignature);

        cmd.SetComputeRootConstantBufferView(
            0,
            currentResource.GetGPUVirtualAddress(key));

        // Root parameter 1 = SRV table(t0)
        cmd.SetComputeRootDescriptorTable(1, read.ParticleBufferSRVGpu);

        // Root parameter 2 = UAV table(u0/u1)
        cmd.SetComputeRootDescriptorTable(2, write.ParticleBufferUAVGPU);

        // Root parameter 3 = SRV table(t1)
        cmd.SetComputeRootDescriptorTable(3, FieldBuffers.SRVFieldDescriptor);

        // Root parameter 5 = CBV 
        cmd.SetComputeRootConstantBufferView(4,
                currentResource.GetGPUVirtualAddress(CommonBuffers.commonKey));

        // Sync previous frame
        cmd.ResourceBarrierUnorderedAccessView(ParticleBuffers.ComputeBuffers[0]);

        // Emitter Update
        cmd.SetPipelineState(EmitterPSO);
        cmd.Dispatch(1, 1, 1);

        // Sync
        cmd.ResourceBarrierUnorderedAccessView(ParticleBuffers.ComputeBuffers[0]);

        //Particle Update
        cmd.SetPipelineState(ParticlePSO);
        uint threadGroupCount = (ParticleBuffers.particleCount + 255) / 256;
        cmd.Dispatch(threadGroupCount, 1, 1);

        // Ensure UAV writes are visible before later use in vertex shader
        cmd.ResourceBarrierTransition(
            write.ParticleBuffer,
            ResourceStates.UnorderedAccess,
            ResourceStates.NonPixelShaderResource);
    }

}