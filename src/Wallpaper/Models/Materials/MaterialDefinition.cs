
using Models;
using Renderer.FrameManagement;
using Renderer.Shaders;
using ShaderConventions;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Models.Material;

public class MaterialDefinition : IDisposable
{
    public RootSignatureDefinition RootSignatureDefinition { get; }
    public ID3D12PipelineState PipelineState { get; }
    public MaterialPermutationKey PermutationKey { get; }
    public DepthDefinition DepthDefinition { get; }

    public AlphaMode alphaMode {get => PermutationKey.AlphaMode;}

    public MaterialDefinition(
        InitContext initContext,
        RootSignatureDefinition rootSignatureDefinition,
        String shaderPath,
        MaterialPermutationKey permutationKey,
        DepthDefinition depthDefinition
        )
    {
        PermutationKey = permutationKey.withDepthPass(false);
        RootSignatureDefinition = rootSignatureDefinition;
        var device = initContext.GraphicsContext.Device;
        PipelineState = CreateGraphicPipeline(device, RootSignatureDefinition.RootSignature, shaderPath);
        DepthDefinition = depthDefinition;
        
    }

    public void Bind(FrameResource frameResource)
    {
        var cmd = frameResource.CommandList;
        cmd.SetPipelineState(PipelineState);
        RootSignatureDefinition.Bind(cmd);
    }
    public void BindDepthPass(FrameResource frameResource)
    {
        DepthDefinition.Bind(frameResource);
    }

    public void Dispose()
    {
        PipelineState.Dispose();
    }

    private ID3D12PipelineState CreateGraphicPipeline(
        ID3D12Device device,
        ID3D12RootSignature rootSignature,
        String shaderPath)
    {

        ReadOnlyMemory<byte> vs = ShaderLibrary.GetShader(shaderPath, "vs", PermutationKey.ShaderPermutation);
        ReadOnlyMemory<byte> ps = ShaderLibrary.GetShader(shaderPath, "ps", PermutationKey.ShaderPermutation);

        var RasterizerState = new RasterizerDescription(
            PermutationKey.TwoSided ? CullMode.None : CullMode.Back,
            FillMode.Solid,
            true);


        GraphicsPipelineStateDescription pipelineStateDescription = new()
        {
            RootSignature = rootSignature,
            VertexShader = vs,
            PixelShader = ps,
            InputLayout = GetInputLayoutDescription().ToArray(),
            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            RasterizerState = RasterizerState,
            BlendState = alphaMode switch
            {
                AlphaMode.OPAQUE => BlendDescription.Opaque,
                AlphaMode.MASK => BlendDescription.Opaque,
                AlphaMode.BLEND => BlendDescription.AlphaBlend,
                _ => throw new NotImplementedException("Unknown alpha mode"),
            },
            DepthStencilState = new DepthStencilDescription(true, DepthWriteMask.All, ComparisonFunction.LessEqual),
            DepthStencilFormat = Format.D32_Float,
            SampleDescription = SampleDescription.Default,
            RenderTargetFormats = [Format.B8G8R8A8_UNorm]
        };

        return device.CreateGraphicsPipelineState(pipelineStateDescription);
    }

    protected IList<InputElementDescription> GetInputLayoutDescription()
    {
        
        var baseLayout = new List<InputElementDescription>
        {
            new("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0),
            new("TANGENT", 0, Format.R32G32B32A32_Float, 24, 0, InputClassification.PerVertexData, 0),
            new("TEXCOORD", 0, Format.R32G32_Float, 40, 0, InputClassification.PerVertexData, 0),
        };

        if(PermutationKey.ShaderPermutation.Skeletal)
        {
            baseLayout.AddRange([
                new("WEIGHTS", 0, Format.R16G16B16A16_UNorm, 48, 0, InputClassification.PerVertexData, 0),
                new("JOINTS", 0, Format.R16G16B16A16_UInt, 56, 0, InputClassification.PerVertexData, 0)
            ]);
        }

        return baseLayout;
    }
}