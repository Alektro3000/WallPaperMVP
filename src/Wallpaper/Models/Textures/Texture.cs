
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Models;

public class Texture : IDisposable
{
    public required String Name;
    public required ID3D12Resource TextureResource;
    public required Format Format;

    public int Width;
    public int Height;

    public void Dispose()
    {
        TextureResource.Dispose();
    }    
}
