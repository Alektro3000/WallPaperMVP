using System.Diagnostics;
using System.Numerics;

public class WhirlController
{
    private readonly ParticleBuffers ParticleSystem;


    public WhirlController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref WhirlConstants constant, FrameMetric frameMetric)
    {
        // Update static buffer
        constant.ViewMatrix =
            Matrix4x4.Transpose(
                Matrix4x4.CreateTranslation(0, 0.2f, 0) *
                Matrix4x4.CreateScale((float)frameMetric.height / frameMetric.width, 1, 1)
                );
        constant.DeltaTime = frameMetric.DeltaTime;
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 3f;
        constant.SpawnRate = 300f;
        constant.FrameIndex = frameMetric.FrameIndex;
    }
}