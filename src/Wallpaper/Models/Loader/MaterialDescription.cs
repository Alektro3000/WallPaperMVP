
using System.Numerics;
using Renderer.FrameManagement;

namespace Models;
public class MaterialDescription
{
    public string Name = "";
    public Vector4 BaseColorFactor = Vector4.One;
    public Texture? BaseColorTexture;
    public Texture? NormalTexture;
    public bool DoubleSided;
    public string AlphaMode = "OPAQUE";
    public float AlphaCutoff = 0.5f;

    public static readonly MaterialDescription DefaultMaterial = new()
    {
        Name = "__DefaultMaterial",
        BaseColorFactor = new Vector4(1, 1, 1, 1),
        BaseColorTexture = null,
        DoubleSided = false,
        AlphaMode = "OPAQUE",
        AlphaCutoff = 0.5f
    };

}