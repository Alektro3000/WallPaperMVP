using Particles.Core;
using Particles.Resources;
using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Passes;
using Renderer.Shaders;
using Vortice.Direct3D12;
using static Vortice.Direct3D12.D3D12;

namespace Particles.Passes;

public class Compute : ICompute
{

    //Compute
    private ID3D12RootSignature RootSignature;
    private ID3D12PipelineState ParticlePSO;
    private ID3D12PipelineState EmitterPSO;

    private ParticleBuffers ParticleBuffers;
    private Shared.Global.Buffers CommonBuffers;
    private Shared.Field.Buffers FieldBuffers;

    public Compute(ID3D12Device device, ParticleBuffers particleSystem, Particles.Shared.Global.Buffers commonBuffers, Particles.Shared.Field.Buffers fieldBuffers, String ComputePath, String EmitterPath)
    {
        ParticleBuffers = particleSystem;
        FieldBuffers = fieldBuffers;
        CommonBuffers = commonBuffers;
        RootSignature = CreateRootSignature(device);

        ParticlePSO = ShaderLibrary.CreatePSO(device, RootSignature, ComputePath);
        EmitterPSO = ShaderLibrary.CreatePSO(device, RootSignature, EmitterPath);
    }

    public void Dispose()
    {
        RootSignature.Dispose();
        ParticlePSO.Dispose();
        EmitterPSO.Dispose();
    }

    protected virtual ID3D12RootSignature CreateRootSignature(ID3D12Device device)
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

        return ShaderLibrary.CreateRootSignature(device, rootParams, [staticSampler]);


    }


    public virtual void DispatchParticles(FrameResource currentResource, FrameManager.ConstantKey key)
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
        cmd.SetComputeRootDescriptorTable(1, read.ParticleBufferSRV.Gpu);

        // Root parameter 2 = UAV table(u0/u1)
        cmd.SetComputeRootDescriptorTable(2, write.ParticleBufferUAV.Gpu);

        // Root parameter 3 = SRV table(t1)
        cmd.SetComputeRootDescriptorTable(3, FieldBuffers.SRVFieldDescriptor);

        // Root parameter 5 = CBV 
        cmd.SetComputeRootConstantBufferView(4,
                currentResource.GetGPUVirtualAddress(CommonBuffers.commonKey));

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

        // Ensure UAV writes are visible before later use in vertex shader
        cmd.ResourceBarrierTransition(
            write.ParticleBuffer,
            ResourceStates.UnorderedAccess,
            ResourceStates.NonPixelShaderResource);
    }

}