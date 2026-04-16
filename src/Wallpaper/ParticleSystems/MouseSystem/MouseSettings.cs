using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct MouseSettings
{
    public Vector3 Color = new Vector3(0.9f, 0.2f, 1f);
    public float Size = 0.016f;
    
    public Vector2 GridSize;
    public float Velocity = 0.05f;
    public float LifeTime = 1f;
    
    public float SpawnRate = 300f;
    public float SpawnRatePerUnit = 100f;
    public float InitSpeed = 40f;

    public MouseSettings(float Size = 0.016f) : this()
    {
        this.Size = Size;
        GridSize = new Vector2(Size); 
    }
}