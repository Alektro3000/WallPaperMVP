
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Renderer.Resources;

public interface Texture 
{
    public String Name {get; }
    public ID3D12Resource TextureResource {get;}
    public Format Format {get;}

    public int Width {get;}
    public int Height {get;}

    public void Dispose();
}
