using System.Diagnostics;
using System.Numerics;

public class ParticleController
{
    private float time = 0f;
    private readonly ParticleBuffers ParticleSystem;
    private int _width;
    private int _height;

    Stopwatch timer = Stopwatch.StartNew();
    double previousTime;
    uint FrameIndex;

    public ParticleController(ParticleBuffers partcileSystem, int width, int height)
    {
        _width = width;
        _height = height;
        ParticleSystem = partcileSystem;
        previousTime = timer.Elapsed.TotalSeconds;
    }

    public void UpdateStaticResource(ref Constants constant)
    {
        double currentTime = timer.Elapsed.TotalSeconds;
        float deltaTime = (float)(currentTime - previousTime);
        previousTime = currentTime;

        // Update static buffer
        FrameIndex++;
        time += deltaTime;
        float t = (float)(Math.Sin(time * 0.2) * 0.5 + 0.5);

        Win32.GetCursorPos(out Win32.POINT point);
        var MousePos = new Vector2(((float)point.X) / _width, (_height - (float)point.Y) / _height) * 2 - new Vector2(1, 1);
        constant.ViewMatrix = Matrix4x4.CreateScale((float)_height/_width,1,1);
        constant.MousePos = MousePos;
        constant.TintColor = new Vector4((MousePos.X+1)/2, (MousePos.Y+1)/2, 1.0f, 1.0f);
        constant.DeltaTime = deltaTime;
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = 3f;
        constant.SpawnRate = 300f;
        constant.FrameIndex = FrameIndex;
    }
}