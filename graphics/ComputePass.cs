using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Dxc;
using static Vortice.Direct3D12.D3D12;
using System.Windows.Markup;
class ComputePass : IDisposable
{
    const int Shader4ComponentMapping = 5768;

    private ID3D12Device _device;

    //Compute
    private ID3D12RootSignature _computeRootSignature;
    private ID3D12PipelineState _computePSO;
    private ID3D12DescriptorHeap _srvUavHeap;

    private ParticleBuffers _particleSystem;
    private struct ParticleBufferBinding
    {
        public CpuDescriptorHandle _srvCpu;
        public GpuDescriptorHandle _srvGpu;
        public CpuDescriptorHandle _uavCpu;
        public GpuDescriptorHandle _uavGpu;
        public ID3D12Resource buffer;
    };
    

    ParticleBufferBinding[] _bufferBindings;

    public ComputePass(ID3D12Device device, ParticleBuffers particleSystem)
    {
        _device = device;
        _particleSystem = particleSystem;
        CreateComputePipeline();
        CreateParticleBufferViews();
    }

    public void Dispose()
    {

    }

    private void CreateComputePipeline()
    {
        //
        // 2. Root signature
        //
        var ranges = new[]
        {
            new DescriptorRange1(
                DescriptorRangeType.ShaderResourceView,
                1,   // one SRV
                0,   // t0 
                0,
                (uint)DescriptorRangeFlags.None,
                0),

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

            // t0 descriptor table
            new RootParameter1(new RootDescriptorTable1(ranges[0]), ShaderVisibility.All),

            // u0 descriptor table
            new RootParameter1(new RootDescriptorTable1(ranges[1]), ShaderVisibility.All)
        };

        var rootSigDesc = new VersionedRootSignatureDescription(
            new RootSignatureDescription1(
                RootSignatureFlags.None,
                rootParams,
                null));

        Vortice.Direct3D.Blob signatureBlob;
        string error = D3D12SerializeVersionedRootSignature(rootSigDesc, out signatureBlob);

        if (signatureBlob == null)
        {
            throw new InvalidOperationException(error);
        }

        _computeRootSignature = _device.CreateRootSignature(0, signatureBlob);

        //
        // 3. Compile shader
        //
        ReadOnlyMemory<byte> computeShader = ShaderHelper.PreCompile("compute.hlsl", DxcShaderStage.Compute);

        //
        // 4. Compute PSO
        //
        var psoDesc = new ComputePipelineStateDescription
        {
            RootSignature = _computeRootSignature,
            ComputeShader = computeShader,
            NodeMask = 0,
            CachedPSO = default,
            Flags = PipelineStateFlags.None
        };

        _computePSO = _device.CreateComputePipelineState(psoDesc);
    }

    private unsafe void CreateParticleBufferViews()
    {
        var _cbvSrvUavDescriptorSize =
        _device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

        //
        // 1. Descriptor heap for SRV/UAV
        //
        var heapDesc = new DescriptorHeapDescription(
            DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            (uint)ParticleBuffers.particleBuffersLength * 2,
            DescriptorHeapFlags.ShaderVisible,
            0);
        _bufferBindings = new ParticleBufferBinding[ParticleBuffers.particleBuffersLength ];

        uint stride = (uint)sizeof(Particle);
        _srvUavHeap = _device.CreateDescriptorHeap(heapDesc);

        var _baseCpu = _srvUavHeap.GetCPUDescriptorHandleForHeapStart();
        var _baseGpu = _srvUavHeap.GetGPUDescriptorHandleForHeapStart();


        var srvDesc = new ShaderResourceViewDescription
        {
            ViewDimension = ShaderResourceViewDimension.Buffer,
            Shader4ComponentMapping = Shader4ComponentMapping,
            Format = Format.Unknown,
            Buffer = new BufferShaderResourceView
            {
                FirstElement = 0,
                NumElements = _particleSystem._particleCount,
                StructureByteStride = stride,
                Flags = BufferShaderResourceViewFlags.None
            }
        };

        var uavDesc = new UnorderedAccessViewDescription
        {
            ViewDimension = UnorderedAccessViewDimension.Buffer,
            Format = Format.Unknown,
            Buffer = new BufferUnorderedAccessView
            {
                FirstElement = 0,
                NumElements = _particleSystem._particleCount,
                StructureByteStride = stride,
                CounterOffsetInBytes = 0,
                Flags = BufferUnorderedAccessViewFlags.None
            }
        };

        for (int i = 0; i < ParticleBuffers.particleBuffersLength; i++)
        {
            var _srvCpu = new CpuDescriptorHandle(_baseCpu, i * 2, _cbvSrvUavDescriptorSize);
            var _srvGpu = new GpuDescriptorHandle(_baseGpu, i * 2, _cbvSrvUavDescriptorSize);

            var _uavCpu = new CpuDescriptorHandle(_srvCpu, 1, _cbvSrvUavDescriptorSize);
            var _uavGpu = new GpuDescriptorHandle(_srvGpu, 1, _cbvSrvUavDescriptorSize);
            var buffer =_particleSystem[i];

            _device.CreateShaderResourceView(buffer, srvDesc, _srvCpu);

            _device.CreateUnorderedAccessView(buffer, null, uavDesc, _uavCpu);

            var bufferBinding = new ParticleBufferBinding()
            {
                _srvCpu = _srvCpu,
                _srvGpu = _srvGpu,

                _uavGpu = _uavGpu,
                _uavCpu = _uavCpu,
                buffer = buffer,
            };

            _bufferBindings[i] = bufferBinding;
        }
    }

    public void DispatchParticles(
    ID3D12GraphicsCommandList cmd,
    FrameResource constantBuffer)
    {
        var read = _bufferBindings[_particleSystem.ReadIndex];
        var write = _bufferBindings[_particleSystem.WriteIndex];
        // Transition particle buffers into correct states.
        cmd.ResourceBarrierTransition(
            read.buffer,
            ResourceStates.VertexAndConstantBuffer,   // or NonPixelShaderResource if already there
            ResourceStates.NonPixelShaderResource);

        cmd.ResourceBarrierTransition(
            write.buffer,
            ResourceStates.VertexAndConstantBuffer,   // or whatever it was before
            ResourceStates.UnorderedAccess);

        cmd.SetDescriptorHeaps(_srvUavHeap);
        cmd.SetComputeRootSignature(_computeRootSignature);
        cmd.SetPipelineState(_computePSO);

        cmd.SetComputeRootConstantBufferView(
            0,
            constantBuffer.ConstantBuffer.GPUVirtualAddress);

        // Root parameter 1 = SRV table(t0)
        cmd.SetComputeRootDescriptorTable(1, read._srvGpu);

        // Root parameter 2 = UAV table(u0)
        cmd.SetComputeRootDescriptorTable(2, write._uavGpu);

        uint threadGroupCount = (_particleSystem._particleCount + 255) / 256;
        cmd.Dispatch(threadGroupCount, 1, 1);

        // Ensure UAV writes are visible before later use.
        cmd.ResourceBarrier(new ResourceBarrier(new ResourceUnorderedAccessViewBarrier(write.buffer)));

        // Example: if you will render from _particleBufferWrite afterward.
        cmd.ResourceBarrierTransition(
            write.buffer,
            ResourceStates.UnorderedAccess,
            ResourceStates.VertexAndConstantBuffer);

        cmd.ResourceBarrierTransition(
            read.buffer,
            ResourceStates.NonPixelShaderResource,
            ResourceStates.VertexAndConstantBuffer);

    }

}