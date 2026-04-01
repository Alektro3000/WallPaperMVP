using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Dxc;
using static Vortice.Direct3D12.D3D12;
using System.Windows.Markup;
using Vortice.Mathematics;
using System.Data.Common;
using System.Numerics;
class GraphicPass : IDisposable
{
    ID3D12Device _device;
    private readonly int _width;
    private readonly int _height;

    // Graphigs Pipeline
    private ID3D12RootSignature _rootSignature;
    private ID3D12PipelineState _pipelineState;

    private Viewport _viewport;
    private RectI _scissor;

    private ParticleBuffers _particleSystem;
    public GraphicPass(ID3D12Device iD3D12Device, ImmidiateCommandList commandList, ParticleBuffers particleSystem, int width, int height)
    {
        _width = width;
        _height = height;
        _device = iD3D12Device;
        _particleSystem = particleSystem;
        CreateGraphicPipeline();
    }
    public void Dispose()
    {
        throw new NotImplementedException();
    }
    
    private void CreateGraphicPipeline()
    {
        var rootParameters = new[]
        {
            new RootParameter1(
                new RootDescriptorTable1(
                    new[]
                    {
                        new DescriptorRange1(
                            DescriptorRangeType.ConstantBufferView,
                            1,   // one CBV
                            0)   // b0
                    }),
                ShaderVisibility.Vertex)
        };

        RootSignatureDescription1 rootSignatureDesc = new(
            RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters);


        _rootSignature = _device.CreateRootSignature(rootSignatureDesc);

        InputElementDescription[] inputElements =
        [
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
        
            // slot 1: one entry per particle instance
            new InputElementDescription(
                "INSTANCE_POSITION",
                0,
                Format.R32G32B32_Float,
                0,
                1,
                InputClassification.PerInstanceData,
                1),

            new InputElementDescription(
                "INSTANCE_VELOCITY",
                0,
                Format.R32G32B32_Float,
                12,
                1,
                InputClassification.PerInstanceData,
                1),

            new InputElementDescription(
                "INSTANCE_COLOR",
                0,
                Format.R32G32B32_Float,
                24,
                1,
                InputClassification.PerInstanceData,
                1),
        ];

        ReadOnlyMemory<byte> vs = ShaderHelper.PreCompile("vertex.hlsl", DxcShaderStage.Vertex);
        ReadOnlyMemory<byte> ps = ShaderHelper.PreCompile("pixel.hlsl", DxcShaderStage.Pixel);

        GraphicsPipelineStateDescription pipelineStateDescription = new GraphicsPipelineStateDescription
        {
            RootSignature = _rootSignature,
            VertexShader = vs,
            PixelShader = ps,

            InputLayout = new InputLayoutDescription(inputElements),

            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,

            RasterizerState = RasterizerDescription.CullNone,
            BlendState = BlendDescription.Opaque,
            DepthStencilState = DepthStencilDescription.None,

            SampleDescription = SampleDescription.Default,

            RenderTargetFormats = [Format.B8G8R8A8_UNorm]
        };

        _pipelineState = _device.CreateGraphicsPipelineState(pipelineStateDescription);

        _viewport = new Viewport(0, 0, _width, _height, 0.0f, 1.0f);
        _scissor = new RectI(0, 0, _width, _height);
    }

    public void Render(ID3D12GraphicsCommandList cmd, FrameResource currentResource, ID3D12Resource particleBuffer)
    {
        // Begin of Graphics Pass
        cmd.RSSetViewport(_viewport);
        cmd.RSSetScissorRect(_scissor);
        cmd.SetPipelineState(_pipelineState);
        cmd.SetGraphicsRootSignature(_rootSignature);
        
        cmd.SetDescriptorHeaps(currentResource.ConstantBufferHeap);
        cmd.SetGraphicsRootDescriptorTable(
            0,
            currentResource.ConstantBufferHeap.GetGPUDescriptorHandleForHeapStart());
            
        cmd.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
        var particleBufferViewWrite = BufferHelper.CreateVertexBufferView<Particle>(particleBuffer, _particleSystem._particleCount);
        cmd.IASetVertexBuffers(0, [_particleSystem.VertexBufferView, particleBufferViewWrite]);
        cmd.IASetIndexBuffer(_particleSystem.IndexBufferView);
        
        cmd.DrawIndexedInstanced(6, _particleSystem._particleCount, 0, 0, 0);
    }
}