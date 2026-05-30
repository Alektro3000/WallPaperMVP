using System.Numerics;
using System.Runtime.InteropServices;
using Models.Material;
using Vortice.Direct3D12;

namespace Models;
public sealed class Primitive
{
    public required IndexBufferView IndexBufferView;
    public required int IndexCount;
    public required VertexBufferView VertexBufferView;
    public required int VertexCount;
    public required MaterialInstance Material;
    public required MaterialDefinition MaterialDefinition;
}
