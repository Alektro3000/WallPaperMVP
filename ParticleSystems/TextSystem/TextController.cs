using System.Diagnostics;
using System.Numerics;

public class TextController
{
    private readonly ParticleBuffers ParticleSystem;


    public TextController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref TextConstants constant, FrameMetric frameMetric)
    {
        // Update static buffer
        constant.ViewMatrix =
            Matrix4x4.Transpose(
                Matrix4x4.CreateScale((float)frameMetric.height / frameMetric.width, 1, 1)
                );
        constant.DeltaTime = frameMetric.DeltaTime;
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 3f;
        constant.SpawnRate = 1000f;
        constant.FrameIndex = frameMetric.FrameIndex;
    }
}