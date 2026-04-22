

using Vortice.Direct3D12;
using static Vortice.Direct3D12.D3D12;

class ShaderHelper
{
    private ShaderHelper(){}
    public static ReadOnlyMemory<byte> GetShader(string path)
    {
        string shadersRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shaders");
        string fullPath = Path.ChangeExtension(Path.Combine(shadersRoot, path),".cso");

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }

        return File.ReadAllBytes(fullPath);
    }
    public static ID3D12PipelineState CreatePSO(ID3D12Device device, ID3D12RootSignature RootSignature, string path)
    {
        var shader = GetShader(path);
        var pipelineState = device.CreateComputePipelineState(new ComputePipelineStateDescription
        {
            RootSignature = RootSignature,
            ComputeShader = shader,
            NodeMask = 0,
            CachedPSO = default,
            Flags = PipelineStateFlags.None
        });
        pipelineState.Name = path;
        return pipelineState;
    }

    public static ID3D12RootSignature CreateRootSignature(ID3D12Device device, RootParameter1[] rootParams, StaticSamplerDescription[] staticSamplers)
    {
        var rootSigDesc = new VersionedRootSignatureDescription(
            new RootSignatureDescription1(
                RootSignatureFlags.None,
                rootParams,
                staticSamplers));

        Vortice.Direct3D.Blob signatureBlob;
        string error = D3D12SerializeVersionedRootSignature(rootSigDesc, out signatureBlob);

        if (signatureBlob == null)
        {
            throw new InvalidOperationException(error);
        }

        return device.CreateRootSignature(0, signatureBlob);
    }
}