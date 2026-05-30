using System.Numerics;
using System.Runtime.InteropServices;
using Renderer.Resources;

namespace Models;

public sealed class Mesh : IDisposable
{
    
    public String Name = "";
    public List<Primitive> Primitives = [];
    public ConstantBufferKey<MeshConstantBuffer> constantBufferKey;

    public void Dispose()
    {
    }
}