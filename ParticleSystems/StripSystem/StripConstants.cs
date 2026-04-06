
using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct StripConstants
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StripDescription
    {
        public Vector2 position;
        public Vector2 size;
        public StripDescription(float positionX, float positionY, float sizeX, float sizeY)
        {
            position = new Vector2(positionX, positionY);
            size = new Vector2(sizeX, sizeY);
        }
    }
    public Matrix4x4 ViewMatrix;
    public float DeltaTime;
    public uint FrameIndex;
    public float LifeTime;
    public uint ParticleCount;
    public StripDescription strip0;
    public StripDescription strip1;
    public StripDescription strip2;
    public StripDescription strip3;
    public StripDescription strip4;
    public Vector3 Color;
    public float SpawnRate;
    public float Acceleration;
    public float Size;
    //public fixed float stripPositionY[5];
}