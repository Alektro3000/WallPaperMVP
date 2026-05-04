using Particles.Core;
using Particles.Resources;
using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Shaders;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Particles.Passes;

public class Graphic : IDisposable
{

    // Graphigs Pipeline
    private ID3D12RootSignature RootSignature;
    private ID3D12PipelineState PipelineState;
    private ID3D12CommandSignature DrawCommandSignature;


    private ParticleBuffers ParticleBuffers;
    private GeometryBuffers GeometryBuffers;
    private Shared.Global.Buffers CommonBuffers;
    public Graphic(
        ID3D12Device device, 
        ParticleBuffers particleSystem, 
        Shared.Global.Buffers commonBuffers, 
        GeometryBuffers geometryBuffers,
        String VertexShaderPath,
        String PixelShaderPath)
    {
        ParticleBuffers = particleSystem;
        GeometryBuffers = geometryBuffers;
        CommonBuffers = commonBuffers;
        DrawCommandSignature = CreateCommandSignature(device);
        RootSignature = CreateRootSignature(device);
        PipelineState = CreateGraphicPipeline(device, VertexShaderPath, PixelShaderPath);
    }
    public void Dispose()
    {
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
                    [
                        new DescriptorRange1(
                            DescriptorRangeType.ShaderResourceView,
                            1,   // one SRV
                            0)   // t0
                    ]),
                ShaderVisibility.Vertex),
            new RootParameter1(
                RootParameterType.ConstantBufferView,
                new RootDescriptor1(1, 0),
                ShaderVisibility.Vertex),
        };
        
        return ShaderLibrary.CreateRootSignature(device, rootParameters, [], RootSignatureFlags.AllowInputAssemblerInputLayout);
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

            RasterizerState = RasterizerDescription.CullNone,
            BlendState = BlendDescription.Additive,
            DepthStencilState = DepthStencilDescription.None,

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
            ByteStride = System.Runtime.InteropServices.Marshal.SizeOf<DrawIndexedArguments>(),
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
                Format.R32G32_Float,
                0,
                0,
                InputClassification.PerVertexData,
                0),

            new InputElementDescription(
                "TEXCOORD",
                0,
                Format.R32G32_Float,
                8,
                0,
                InputClassification.PerVertexData,
                0),
        
        ]);
        
    }

    public void Render(FrameResource currentResource, IConstantBufferKey key)
    {
        var cmd = currentResource.CommandList;
        // Begin of Graphics Pass
        cmd.SetPipelineState(PipelineState);
        cmd.SetGraphicsRootSignature(RootSignature);
        
        cmd.SetGraphicsRootConstantBufferView(
            0,
            currentResource.GetGPUVirtualAddress(key));
        cmd.SetGraphicsRootDescriptorTable(
            1, 
            ParticleBuffers.WriteBufferBinding.ParticleBufferSRV.Gpu);
        cmd.SetGraphicsRootConstantBufferView(
            2, 
            currentResource.GetGPUVirtualAddress(CommonBuffers.commonKey));

        cmd.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
        cmd.IASetVertexBuffers(0, [GeometryBuffers.VertexBufferView]);
        cmd.IASetIndexBuffer(GeometryBuffers.IndexBufferView);
        
        //cmd.DrawIndexedInstanced(GeometryBuffers.IndexCount, ParticleBuffers.particleCount, 0, 0, 0);
        cmd.ExecuteIndirect(DrawCommandSignature, 1, ParticleBuffers.DrawArgs, 0, null, 0);
    }
}