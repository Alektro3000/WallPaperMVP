
using System.Runtime.InteropServices;
using Renderer;
using Renderer.Commands;
using Renderer.Core;
using Renderer.Descriptors;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Shaders;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Models;

[Shader("models\\materials\\base\\pixel.hlsl", "ps")]
[Shader("models\\materials\\base\\vertex.hlsl", "vs")]
public class Material : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MaterialInfo
    {
        public uint flags;
    }
    // Graphigs Pipeline
    private ID3D12RootSignature RootSignature;
    private ID3D12PipelineState PipelineState;
    private ID3D12CommandSignature DrawCommandSignature;

    public Texture? AlbedoTexture;
    public Texture? NormalTexture;

    private ConstantBufferKey<MaterialInfo> ConstantKey;

    public Material(
        InitContext initContext,
        MaterialDescription materialDescription)
    {
        var device = initContext.GraphicsContext.Device;
        DrawCommandSignature = CreateCommandSignature(device);
        RootSignature = CreateRootSignature(device);
        PipelineState = CreateGraphicPipeline(device, "models\\materials\\base\\vertex.hlsl", "models\\materials\\base\\pixel.hlsl");


        AlbedoTexture = materialDescription.BaseColorTexture;
        NormalTexture = materialDescription.NormalTexture;
        ConstantKey = initContext.ConstantBufferRegistry.Reserve<MaterialInfo>("Material Constant Buffer");
    }
    public void Dispose()
    {
        NormalTexture?.Dispose();
        AlbedoTexture?.Dispose();

        RootSignature?.Dispose();
        PipelineState?.Dispose();
        DrawCommandSignature?.Dispose();
    }

    private ID3D12RootSignature CreateRootSignature(ID3D12Device device)
    {
        var rootParameters = new[]
        {
            new RootParameter1(
                RootParameterType.ConstantBufferView,
                new RootDescriptor1(0, 0),
                ShaderVisibility.Vertex),
                
            new RootParameter1(
                new RootDescriptorTable1(
                new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 0)), 
                ShaderVisibility.All),
                
            new RootParameter1(
                RootParameterType.ConstantBufferView,
                new RootDescriptor1(1, 0),
                ShaderVisibility.Pixel),

            new RootParameter1(
                new RootDescriptorTable1(
                new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 1)), 
                ShaderVisibility.All),

            new RootParameter1(
                RootParameterType.ConstantBufferView,
                new RootDescriptor1(2, 0),
                ShaderVisibility.Vertex
            )
        };

        var staticSampler = new StaticSamplerDescription(ShaderVisibility.All, 0, 0)
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            ComparisonFunction = ComparisonFunction.Never,
            MaxLOD = float.MaxValue
        };

        return ShaderLibrary.CreateRootSignature(device, rootParameters, [staticSampler], RootSignatureFlags.AllowInputAssemblerInputLayout);
    }
    private ID3D12PipelineState CreateGraphicPipeline(
        ID3D12Device device,
        String VertexShaderPath,
        String PixelShaderPath)
    {

        ReadOnlyMemory<byte> vs = ShaderLibrary.GetShader(VertexShaderPath);
        ReadOnlyMemory<byte> ps = ShaderLibrary.GetShader(PixelShaderPath);

        GraphicsPipelineStateDescription pipelineStateDescription = new GraphicsPipelineStateDescription
        {
            RootSignature = RootSignature,
            VertexShader = vs,
            PixelShader = ps,

            InputLayout = GetInputLayoutDescription(),

            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,

            RasterizerState = RasterizerDescription.CullClockwise,
            BlendState = BlendDescription.Opaque,
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = Format.D32_Float,

            SampleDescription = SampleDescription.Default,

            RenderTargetFormats = [Format.B8G8R8A8_UNorm]
        };

        return device.CreateGraphicsPipelineState(pipelineStateDescription);

    }
    private ID3D12CommandSignature CreateCommandSignature(ID3D12Device device)
    {
        var argDescs = new[]
        {
            new IndirectArgumentDescription
            {
                Type = IndirectArgumentType.DrawIndexed
            }
        };

        var cmdSigDesc = new CommandSignatureDescription(argDescs)
        {
            ByteStride = Marshal.SizeOf<DrawIndexedArguments>(),
        };

        return device.CreateCommandSignature<ID3D12CommandSignature>(cmdSigDesc, null);
    }

    private InputLayoutDescription GetInputLayoutDescription()
    {
        return new InputLayoutDescription([
            // slot 0: shared quad vertices
            new InputElementDescription(
                "POSITION",
                0,
                Format.R32G32B32_Float,
                0,
                0,
                InputClassification.PerVertexData,
                0),

            new InputElementDescription(
                "NORMAL",
                0,
                Format.R32G32B32_Float,
                12,
                0,
                InputClassification.PerVertexData,
                0),

            new InputElementDescription(
                "TANGENT",
                0,
                Format.R32G32B32A32_Float,
                24,
                0,
                InputClassification.PerVertexData,
                0),


            new InputElementDescription(
                "TEXCOORD",
                0,
                Format.R32G32_Float,
                40,
                0,
                InputClassification.PerVertexData,
                0),

            new InputElementDescription(
                "WEIGHTS",
                0,
                Format.R16G16B16A16_UNorm,
                48,
                0,
                InputClassification.PerVertexData,
                0),
                
            new InputElementDescription(
                "JOINTS",
                0,
                Format.R16G16B16A16_UInt,
                56,
                0,
                InputClassification.PerVertexData,
                0),

        ]);

    }

    public void BindMaterial(FrameResource frameResource)
    {
        var cmd = frameResource.CommandList;

        ref var materialConstantBuffer = ref frameResource.GetBufferConstantRef(ConstantKey);

        var settings = frameResource.Settings.GetSettings<Settings>();
        var flags = (AlbedoTexture != null) ? 1u : 0;
        flags |= (NormalTexture != null) ? 2u : 0;
        flags |= (settings.showUV > 0) ? 4u : 0;
        flags |= (settings.showNormal > 0) ? 8u : 0;

        materialConstantBuffer.flags = flags;

        // Begin of Graphics Pass
        cmd.SetPipelineState(PipelineState);
        cmd.SetGraphicsRootSignature(RootSignature);


        cmd.SetGraphicsRootConstantBufferView(2, frameResource.GetGPUVirtualAddress(ConstantKey));

        if(AlbedoTexture != null)
            cmd.SetGraphicsRootDescriptorTable(1, AlbedoTexture.Handle.Gpu);

        if(NormalTexture != null)
            cmd.SetGraphicsRootDescriptorTable(3, NormalTexture.Handle.Gpu);

    }
}
