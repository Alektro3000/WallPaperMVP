using System.Numerics;

public struct MouseSettings
{
    public Vector3 Color = new Vector3(0.9f, 0.2f, 1f);
    public float Size = 0.025f;
    public float LifeTime = 0.5f;
    public float SpawnRate = 1000f;
    public float SpawnRatePerUnit = 50f;
    public float Velocity = 0.3f;

    public MouseSettings()
    {
    }
}