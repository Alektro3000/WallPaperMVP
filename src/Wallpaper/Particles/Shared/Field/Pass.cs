using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Shaders;
using Vortice.Direct3D12;
using static Vortice.Direct3D12.D3D12;

namespace Particles.Shared.Field;

[Shader("field.hlsl", "cs")]
sealed public class Pass : IDisposable
{

    //Compute
    private ID3D12RootSignature RootSignature;
    private ID3D12PipelineState FieldPSO;
    private Buffers FieldBuffers;
    private Global.Buffers CommonBuffers;

    public Pass(ID3D12Device device, Buffers fieldBuffers, Global.Buffers commonBuffers, String FieldPath)
    {
        FieldBuffers = fieldBuffers;
        CommonBuffers = commonBuffers;
        RootSignature = CreateRootSignature(device);
        FieldPSO = ShaderLibrary.CreatePSO(device, RootSignature, FieldPath);
    }

    public void Dispose()
    {
        RootSignature.Dispose();
    }

    private ID3D12RootSignature CreateRootSignature(ID3D12Device device)
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


        return ShaderLibrary.CreateRootSignature(device, rootParams, []);
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

        cmd.Dispatch((Buffers.width + 7) / 8, (Buffers.height + 7) / 8, 1);

        // Ensure UAV writes are visible before later use in vertex shader
        cmd.ResourceBarrierTransition(
            FieldBuffers.FieldResource,
            ResourceStates.UnorderedAccess,
            ResourceStates.AllShaderResource);
    }

}