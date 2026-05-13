using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Direct3D;
using Renderer.Core;
using Renderer;
using Renderer.FrameManagement;
using Renderer.Shaders;
using Particles.Settings;
using Settings;


namespace Particles.Shared.Field;

[Shader("field/vertex.hlsl", "vs")]
[Shader("field/pixel.hlsl", "ps")]
public class Debug : IDisposable
{

    // Graphigs Pipeline
    private ID3D12RootSignature RootSig;
    private ID3D12PipelineState PSO;

    private Buffers buffers;
    public Debug(
        ID3D12Device device,
        Buffers buffers)
    {
        this.buffers = buffers;
        RootSig = CreateRootSignature(device);
        PSO = CreatePipelineState(device, "field/vertex.hlsl", "field/pixel.hlsl");
    }
    public void Dispose()
    {
        RootSig?.Dispose();
        PSO?.Dispose();
    }

    private ID3D12RootSignature CreateRootSignature(
        ID3D12Device device)
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
        
        var rootParameters = new[]
        {
            // b0 as root CBV
            new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(0, 0), ShaderVisibility.All),
            new RootParameter1(
                new RootDescriptorTable1(
                    new[]
                    {
                        new DescriptorRange1(
                            DescriptorRangeType.ShaderResourceView,
                            1,   // one SRV
                            0)   // t0
                    }),
                ShaderVisibility.Pixel),
        };

        return ShaderLibrary.CreateRootSignature(device, rootParameters, [staticSampler]);
        


    }

    private ID3D12PipelineState CreatePipelineState(
        ID3D12Device device,
        String VertexShaderPath,
        String PixelShaderPath)
    {
        
        ReadOnlyMemory<byte> vs = ShaderLibrary.GetShader(VertexShaderPath);
        ReadOnlyMemory<byte> ps = ShaderLibrary.GetShader(PixelShaderPath);

        GraphicsPipelineStateDescription pipelineStateDescription = new GraphicsPipelineStateDescription
        {
            RootSignature = RootSig,
            VertexShader = vs,
            PixelShader = ps,

            InputLayout = new InputLayoutDescription([]),

            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,

            RasterizerState = RasterizerDescription.CullNone,
            BlendState = BlendDescription.Additive,
            DepthStencilState = DepthStencilDescription.None,

            SampleDescription = SampleDescription.Default,

            RenderTargetFormats = [Format.B8G8R8A8_UNorm]
        };

        return device.CreateGraphicsPipelineState(pipelineStateDescription);
    }

    public void Render(FrameResource currentResource, SystemSettings systemSettings)
    {
        if(systemSettings.GetSettings<Settings>().IsDebugModeEnabled < 0.5f)
            return;

        var cmd = currentResource.CommandList;
        
        // Begin of Graphics Pass
        cmd.SetPipelineState(PSO);
        cmd.SetGraphicsRootSignature(RootSig);

        cmd.SetGraphicsRootConstantBufferView(
            0,
            currentResource.GetGPUVirtualAddress(buffers.fieldKey));
        cmd.SetGraphicsRootDescriptorTable(1, buffers.SRVFieldDescriptor);

        cmd.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        cmd.DrawInstanced(6, 1, 0, 0);
    }
}
