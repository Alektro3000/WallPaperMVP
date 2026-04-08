using System.Diagnostics;
using System.Numerics;

public class CornerController
{
    private readonly ParticleBuffers ParticleSystem;

    public CornerController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref CornerConstants constant, FrameMetric frameMetric)
    {
        // Update static buffer
        float scale = frameMetric.width/(float)frameMetric.height;
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 6f;
        constant.SpawnRate = 70f;
        constant.Color = new Vector3(0.2f, 0.9f, 1f);
        constant.Size = 0.05f;
        constant.SpawnDistribution = new Vector2(0.4f,0.4f);
        constant.SpawnPosition = new Vector2(scale,1) + new Vector2(0.03f);
        constant.RemoveBox = constant.SpawnPosition + new Vector2(0.05f);
    }
}