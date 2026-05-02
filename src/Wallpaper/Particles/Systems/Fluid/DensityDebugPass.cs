using Particles.Resources;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Shaders;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Particles.Systems.Fluid;

public sealed class DensityDebugPass : IDisposable
{
    private readonly ID3D12RootSignature rootSignature;
    private readonly ID3D12PipelineState pipelineState;
    private readonly ParticleComputeBindings bindings;
    private readonly GpuDescriptorHandle fluidSrvs;
    private readonly Shared.Global.Buffers commonBuffers;

    public DensityDebugPass(
        ID3D12Device device,
        ParticleComputeBindings bindings,
        GpuDescriptorHandle fluidSrvs,
        Shared.Global.Buffers commonBuffers,
        string vertexShader,
        string pixelShader)
    {
        this.bindings = bindings;
        this.fluidSrvs = fluidSrvs;
        this.commonBuffers = commonBuffers;
        rootSignature = CreateRootSignature(device);
        pipelineState = CreatePipelineState(device, vertexShader, pixelShader);
    }

    public void Render(FrameResource currentResource, ConstantBufferKey key)
    {
        var cmd = currentResource.CommandList;
        cmd.SetPipelineState(pipelineState);
        cmd.SetGraphicsRootSignature(rootSignature);
        cmd.SetGraphicsRootConstantBufferView(0, currentResource.GetGPUVirtualAddress(key));
        cmd.SetGraphicsRootDescriptorTable(1, bindings.ParticleBuffers.WriteBufferBinding.ParticleBufferSRV.Gpu);
        cmd.SetGraphicsRootDescriptorTable(2, fluidSrvs);
        cmd.SetGraphicsRootConstantBufferView(3, currentResource.GetGPUVirtualAddress(commonBuffers.commonKey));
        cmd.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        cmd.DrawInstanced(6, 1, 0, 0);
    }

    private static ID3D12RootSignature CreateRootSignature(ID3D12Device device)
    {
        var rootParameters = new[]
        {
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(0, 0), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 0)), ShaderVisibility.All),
            new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 2, 4)), ShaderVisibility.All),
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(1, 0), ShaderVisibility.All),
        };

        return ShaderLibrary.CreateRootSignature(device, rootParameters, []);
    }

    private ID3D12PipelineState CreatePipelineState(ID3D12Device device, string vertexShader, string pixelShader)
    {
        return device.CreateGraphicsPipelineState(new GraphicsPipelineStateDescription
        {
            RootSignature = rootSignature,
            VertexShader = ShaderLibrary.GetShader(vertexShader),
            PixelShader = ShaderLibrary.GetShader(pixelShader),
            InputLayout = new InputLayoutDescription([]),
            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            RasterizerState = RasterizerDescription.CullNone,
            BlendState = BlendDescription.Opaque,
            DepthStencilState = DepthStencilDescription.None,
            SampleDescription = SampleDescription.Default,
            RenderTargetFormats = [Format.B8G8R8A8_UNorm]
        });
    }

    public void Dispose()
    {
        pipelineState.Dispose();
        rootSignature.Dispose();
    }
}
