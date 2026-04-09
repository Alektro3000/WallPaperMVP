using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Direct3D;
[Shader("debug.hlsl", "vs")]
[Shader("debugpixel.hlsl", "ps")]
public class DebugPass : IDisposable
{

    // Graphigs Pipeline
    private ID3D12RootSignature debugTextureRootSig;
    private ID3D12PipelineState debugTexturePso;

    private GpuDescriptorHandle DebugTexture;
    public DebugPass(
        ID3D12Device device,
        GpuDescriptorHandle DebugTexture)
    {
        this.DebugTexture = DebugTexture;
        CreateGraphicPipeline(device, "debug.hlsl", "debugpixel.hlsl");
    }
    public void Dispose()
    {
        debugTextureRootSig?.Dispose();
        debugTexturePso?.Dispose();
    }

    private void CreateGraphicPipeline(
        ID3D12Device device,
        String VertexShaderPath,
        String PixelShaderPath)
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

        RootSignatureDescription1 rootSignatureDesc = new(
            RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters, [staticSampler]);


        debugTextureRootSig = device.CreateRootSignature(rootSignatureDesc);

        ReadOnlyMemory<byte> vs = ShaderHelper.GetShader(VertexShaderPath);
        ReadOnlyMemory<byte> ps = ShaderHelper.GetShader(PixelShaderPath);

        GraphicsPipelineStateDescription pipelineStateDescription = new GraphicsPipelineStateDescription
        {
            RootSignature = debugTextureRootSig,
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

        debugTexturePso = device.CreateGraphicsPipelineState(pipelineStateDescription);

    }

    public void Render(FrameResource currentResource)
    {
        var cmd = currentResource.CommandList;
        // Begin of Graphics Pass
        cmd.SetPipelineState(debugTexturePso);
        cmd.SetGraphicsRootSignature(debugTextureRootSig);

        cmd.SetGraphicsRootDescriptorTable(0, DebugTexture);

        cmd.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        cmd.DrawInstanced(6, 1, 0, 0);
    }
}