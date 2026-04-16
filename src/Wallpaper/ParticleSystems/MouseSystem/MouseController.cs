using System.Numerics;

public class MouseController
{
    private readonly ParticleBuffers ParticleSystem;
    private Vector2 prevMousePos;

    public SystemSettings systemSettings;

    public MouseController(ParticleBuffers partcileSystem, SystemSettings settings)
    {
        systemSettings = settings;
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref MouseConstants constant, FrameMetric metric)
    {
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.mouseSettings = systemSettings.mouseSettings;
        
        Win32.GetCursorPos(out Win32.POINT point);
        float ratio = (float)metric.height/metric.width;
        var MousePos = new Vector2(((float)point.X) / metric.height, (metric.height - (float)point.Y) / metric.height) * 2 - new Vector2(1/ratio, 1);

        constant.mousePos = MousePos;
        constant.mousePosPrev = prevMousePos;
        prevMousePos = MousePos;
    }
}