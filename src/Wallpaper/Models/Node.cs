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
    public Matrix4x4 GlobalTransform;
    
    public void UpdateWorldTransforms(Matrix4x4 worldMatrix)
    {
        GlobalTransform = LocalTransform.Matrix * worldMatrix;
        foreach(var child in Children)
            child.UpdateWorldTransforms(GlobalTransform);
    }
}

