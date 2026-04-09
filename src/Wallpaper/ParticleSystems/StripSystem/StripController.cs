using System.Diagnostics;
using System.Numerics;

public class StripController
{
    private readonly ParticleBuffers ParticleSystem;


    public StripController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref StripConstants constant, FrameMetric frameMetric)
    {
        // Update static buffer
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 3f;
        constant.SpawnRate = 500f;
        constant.Color  = new Vector3(0.2f, 0.9f, 1f);
        constant.Size = 0.04f;
        constant.GridSize = new Vector2(0.01f, 0.02f);
        constant.strip0 = CreateStrip(0.1f, 0f, 1f);
        constant.strip1 = CreateStrip(0.2f, -0.1f, 1.1f);
        constant.strip2 = CreateStrip(0.3f, +0.1f, 1.2f);
        constant.strip3 = CreateStrip(0.4f, -0.2f, 1.3f);
        constant.strip4 = CreateStrip(0.5f, +0.2f, 1.3f);
    }

    public StripConstants.StripDescription CreateStrip(float x, float y, float ys)
    {
        return new StripConstants.StripDescription(x+1,y,0.05f,ys);
    }
}