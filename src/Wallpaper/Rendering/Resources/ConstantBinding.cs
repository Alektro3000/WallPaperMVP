
using Vortice.Direct3D12;


namespace Renderer.Resources;

public struct ConstantBinding
{
    public ID3D12Resource ConstantBuffer;

    public unsafe byte* MappedConstants;
    public unsafe ref T Constants<T>() where T : unmanaged => ref *(T*)MappedConstants;
}