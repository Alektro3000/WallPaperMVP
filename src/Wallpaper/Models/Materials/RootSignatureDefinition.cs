using Renderer.Shaders;
using Vortice.Direct3D12;

namespace Models;

public enum RootSignatureDefinitionType
{
    StaticMesh,
    SkeletalMesh
}

public sealed class RootSignatureDefinition : IDisposable
{
    private RootSignatureDefinitionType Type { get; }
    public ID3D12RootSignature RootSignature { get; }

    private BindlessTextureProvider TextureProvider {get; }

    public RootSignatureDefinition(InitContext initContext, RootSignatureDefinitionType type, BindlessTextureProvider textureProvider)
    {
        Type = type;
        TextureProvider = textureProvider;
        var device = initContext.GraphicsContext.Device;
        RootSignature = CreateRootSignature(device, type);
    }

    public void Dispose()
    {
        RootSignature.Dispose();
    }
    public void Bind(ID3D12GraphicsCommandList cmd)
    {
        cmd.SetGraphicsRootSignature(RootSignature);
        cmd.SetGraphicsRootDescriptorTable(2, TextureProvider.GetBindlessTableStart());
    }

    private static ID3D12RootSignature CreateRootSignature(ID3D12Device device, RootSignatureDefinitionType type)
    {
        var rootParameters = new List<RootParameter1>
        {
            new(
                RootParameterType.ConstantBufferView,
                new RootDescriptor1(0, 0),
                ShaderVisibility.Vertex),
            new(
                RootParameterType.ConstantBufferView,
                new RootDescriptor1(1, 0),
                ShaderVisibility.Pixel),
            new(
                new RootDescriptorTable1(
                    new DescriptorRange1(DescriptorRangeType.ShaderResourceView, uint.MaxValue, 0)),
                ShaderVisibility.All)
        };

        if (type == RootSignatureDefinitionType.SkeletalMesh)
        {
            rootParameters.Add(
                new RootParameter1(
                    RootParameterType.ConstantBufferView,
                    new RootDescriptor1(2, 0),
                    ShaderVisibility.Vertex));
        }

        var staticSampler = new StaticSamplerDescription(ShaderVisibility.All, 0, 0)
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            ComparisonFunction = ComparisonFunction.Never,
            MaxLOD = float.MaxValue
        };

        return ShaderLibrary.CreateRootSignature(
            device,
            rootParameters.ToArray(),
            [staticSampler],
            RootSignatureFlags.AllowInputAssemblerInputLayout);
    }
    
    public uint? SkeletalMeshBind()
    {
        return Type == RootSignatureDefinitionType.SkeletalMesh ? 3u : null;
    }
}
