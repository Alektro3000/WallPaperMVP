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
        float scale = (float)frameMetric.height / frameMetric.width;
        constant.ViewMatrix =
            Matrix4x4.Transpose(
                Matrix4x4.CreateScale(scale, 1, 1)
                );
        constant.DeltaTime = frameMetric.DeltaTime;
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 6f;
        constant.SpawnRate = 70f;
        constant.FrameIndex = frameMetric.FrameIndex;
        constant.Color = new Vector3(0.2f, 0.9f, 1f);
        constant.Size = 0.05f;
        constant.SpawnDistribution = new Vector2(0.4f,0.4f);
        constant.SpawnPosition = new Vector2(1/scale,1) + new Vector2(0.03f);
        constant.RemoveBox = constant.SpawnPosition + new Vector2(0.05f);
    }
}