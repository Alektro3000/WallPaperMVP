
using Renderer.Descriptors;
using Vortice.Direct3D12;

namespace Models;

public class Texture : IDisposable
{
    public required String Name;
    public required ID3D12Resource TextureResource;


    public ResourceDescriptor Handle;

    public int Width;
    public int Height;

    public void Dispose()
    {
        TextureResource.Dispose();
    }    
}