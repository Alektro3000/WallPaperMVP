using Vortice.Direct3D12;
using Vortice.DXGI;

public class GraphicPass : IDisposable
{

    // Graphigs Pipeline
    private ID3D12RootSignature _rootSignature;
    private ID3D12PipelineState _pipelineState ;
    private ID3D12CommandSignature _drawCommandSignature;


    private ParticleBuffers ParticleBuffers;
    private GeometryBuffers GeometryBuffers;
    private CommonBuffers CommonBuffers;
    public GraphicPass(
        ID3D12Device device, 
        ParticleBuffers particleSystem, 
        CommonBuffers commonBuffers, 
        GeometryBuffers geometryBuffers,
        String VertexShaderPath,
        String PixelShaderPath)
    {
        ParticleBuffers = particleSystem;
        GeometryBuffers = geometryBuffers;
        CommonBuffers = commonBuffers;
        CreateArgs(device);
        CreateGraphicPipeline(device, VertexShaderPath, PixelShaderPath);
    }
    public void Dispose()
    {
        _rootSignature?.Dispose();
        _pipelineState?.Dispose();
        _drawCommandSignature?.Dispose();
    }
    
    private void CreateGraphicPipeline(
        ID3D12Device device, 
        String VertexShaderPath,
        String PixelShaderPath)
    {
        var rootParameters = new[]
        {
            new RootParameter1(
                RootParameterType.ConstantBufferView,
                new RootDescriptor1(0, 0),
                ShaderVisibility.Vertex),
            new RootParameter1(
                new RootDescriptorTable1(
                    new[]
                    {
                        new DescriptorRange1(
                            DescriptorRangeType.ShaderResourceView,
                            1,   // one SRV
                            0)   // t0
                    }),
                ShaderVisibility.Vertex),
            new RootParameter1(
                RootParameterType.ConstantBufferView,
                new RootDescriptor1(1, 0),
                ShaderVisibility.Vertex),
        };

        RootSignatureDescription1 rootSignatureDesc = new(
            RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters);


        _rootSignature = device.CreateRootSignature(rootSignatureDesc);

        ReadOnlyMemory<byte> vs = ShaderHelper.GetShader(VertexShaderPath);
        ReadOnlyMemory<byte> ps = ShaderHelper.GetShader(PixelShaderPath);

        GraphicsPipelineStateDescription pipelineStateDescription = new GraphicsPipelineStateDescription
        {
            RootSignature = _rootSignature,
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

        _pipelineState = device.CreateGraphicsPipelineState(pipelineStateDescription);

    }
    private void CreateArgs(ID3D12Device device)
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

        _drawCommandSignature = device.CreateCommandSignature<ID3D12CommandSignature>(cmdSigDesc, null);
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

    public void Render(FrameResource currentResource, FrameManager.ConstantKey key)
    {
        var cmd = currentResource.CommandList;
        // Begin of Graphics Pass
        cmd.SetPipelineState(_pipelineState);
        cmd.SetGraphicsRootSignature(_rootSignature);
        
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
        cmd.ExecuteIndirect(_drawCommandSignature, 1, ParticleBuffers.DrawArgs, 0, null, 0);
    }
}