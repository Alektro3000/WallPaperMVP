
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Models;

public class Texture : IDisposable
{
    public required String Name;
    public required ID3D12Resource TextureResource;
    public Format Format = Format.R8G8B8A8_UNorm_SRgb;

    public int Width;
    public int Height;

    public void Dispose()
    {
        TextureResource.Dispose();
    }    
}
