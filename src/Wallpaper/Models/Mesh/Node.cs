using System.Data;
using System.Numerics;

namespace Models;
public sealed class Node
{
    public string Name = "";
    public Node? Parent;
    public Mesh? Mesh;
    public Skin? Skin;
    public List<Node> Children = [];
    
    public AffineTransform DefaultTransform;
    public AffineTransform LocalTransform;
    public AffineTransform GlobalTransform;
    public Matrix4x4 GlobalMatrix;
    
    public void UpdateWorldTransforms(AffineTransform worldMatrix)
    {
        GlobalTransform = LocalTransform * worldMatrix;
        GlobalMatrix = GlobalTransform.Matrix;
        foreach(var child in Children)
            child.UpdateWorldTransforms(GlobalTransform);
    }
}

