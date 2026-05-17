

using System.Drawing.Drawing2D;
using System.Numerics;

public struct AffineTransform{
    
    public Vector3 Translation;
    public Quaternion Rotation;
    public Vector3 Scale;
    
    public AffineTransform( 
        Vector3 Translation,
        Quaternion Rotation,
        Vector3 Scale)
    {
        this.Translation = Translation;
        this.Rotation = Rotation;
        this.Scale = Scale;
    }
    

    public Matrix4x4 Matrix{get
    {
        return Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateTranslation(Translation);

    }}
}