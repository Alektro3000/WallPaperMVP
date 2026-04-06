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
        constant.ViewMatrix =
            Matrix4x4.Transpose(
                Matrix4x4.CreateScale((float)frameMetric.height / frameMetric.width, 1, 1)
                );
        constant.DeltaTime = frameMetric.DeltaTime;
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 3f;
        constant.SpawnRate = 1500f;
        constant.FrameIndex = frameMetric.FrameIndex;
        constant.Color  = new Vector3(0.9f, 0.2f, 1f);
        constant.strip0 = new StripConstants.StripDescription(0.1f, 0f, 0.1f, 1f);
        constant.strip1 = new StripConstants.StripDescription(0.2f, -0.1f, 0.1f, 1.1f);
        constant.strip2 = new StripConstants.StripDescription(0.3f, +0.1f, 0.1f, 1.2f);
        constant.strip3 = new StripConstants.StripDescription(0.4f, -0.2f, 0.1f, 1.3f);
        constant.strip4 = new StripConstants.StripDescription(0.5f, +0.2f, 0.1f, 1.3f);
    }
}