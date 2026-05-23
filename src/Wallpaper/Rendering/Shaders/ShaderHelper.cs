

using ShaderConventions;
using Vortice.Direct3D12;
using static Vortice.Direct3D12.D3D12;

namespace Renderer.Shaders;

static class ShaderLibrary
{
    public static ReadOnlyMemory<byte> GetShader(string path, string stage, PermutationKey key)
    {
        string shadersRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shaders");
        string sourcePath = Path.Combine(shadersRoot, path);
        string fullPath = key.GetFileName(sourcePath, stage);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }

        return File.ReadAllBytes(fullPath);
    }
    public static ReadOnlyMemory<byte> GetShader(string path, string stage)
    {
        return GetShader(path, stage, PermutationKey.Default);
    }
    public static ID3D12PipelineState CreatePSO(ID3D12Device device, ID3D12RootSignature RootSignature, string path)
    {
        var shader = GetShader(path, "cs", PermutationKey.Default);
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

    public static ID3D12RootSignature CreateRootSignature(ID3D12Device device, RootParameter1[] rootParams, StaticSamplerDescription[] staticSamplers, RootSignatureFlags flags = RootSignatureFlags.None)
    {
        var rootSigDesc = new VersionedRootSignatureDescription(
            new RootSignatureDescription1(
                flags,
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
