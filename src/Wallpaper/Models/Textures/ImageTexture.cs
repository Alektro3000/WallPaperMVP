

using Renderer.Resources;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Models;

public class ImageTexture : Texture
{
    public required String Name;
    public required ID3D12Resource TextureResource;
    public required Format Format;

    public int Width;
    public int Height;

    string Texture.Name => Name;

    ID3D12Resource Texture.TextureResource => TextureResource;

    Format Texture.Format => Format;

    int Texture.Width => Width;

    int Texture.Height => Height;

    public void Dispose()
    {
        TextureResource.Dispose();
    }    
}
