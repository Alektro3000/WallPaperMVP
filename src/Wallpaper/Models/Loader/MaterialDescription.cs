
using System.Numerics;
using Renderer.FrameManagement;

namespace Models;

public enum AlphaMode
{
    OPAQUE,
    MASK,
    BLEND
}

public class MaterialDescription
{
    public string Name = "";
    public Vector4? BaseColorFactor;
    public Texture? BaseColorTexture;
    public Texture? NormalTexture;
    public Texture? PackedTexture;
    public bool DoubleSided;
    public float AlphaCutoff = 0.5f;
}