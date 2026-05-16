using System.Data;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpGLTF.Schema2;

namespace Models;
public sealed class Node
{
    public string Name = "";
    public Node? Parent;
    public Mesh? Mesh;
    public Skin? Skin;
    public List<Node> Children = [];
    public Matrix4x4 LocalTransform;
    public Matrix4x4 GlobalTransform;
    
    public void UpdateWorldTransforms(Matrix4x4 worldMatrix)
    {
        GlobalTransform = LocalTransform * worldMatrix;
        foreach(var child in Children)
            child.UpdateWorldTransforms(GlobalTransform);
    }
}

