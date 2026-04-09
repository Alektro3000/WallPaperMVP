using System.Numerics;

public class MouseController
{
    private readonly ParticleBuffers ParticleSystem;
    private Vector2 prevMousePos;

    public MouseSettings mouseSettings = new MouseSettings();

    public MouseController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref MouseConstants constant, FrameMetric metric)
    {
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.LifeTime = mouseSettings.LifeTime;
        constant.SpawnRate = mouseSettings.SpawnRate;
        constant.SpawnRatePerUnit = mouseSettings.SpawnRatePerUnit;
        constant.Color = mouseSettings.Color;
        constant.Velocity = mouseSettings.Velocity;
        
        Win32.GetCursorPos(out Win32.POINT point);
        float ratio = (float)metric.height/metric.width;
        var MousePos = new Vector2(((float)point.X) / metric.height, (metric.height - (float)point.Y) / metric.height) * 2 - new Vector2(1/ratio, 1);

        constant.mousePos = MousePos;
        constant.mousePosPrev = prevMousePos;
        constant.GridSize = new Vector2(1, 1) * mouseSettings.Size;
        constant.Size =  mouseSettings.Size;
        prevMousePos = MousePos;
    }
}