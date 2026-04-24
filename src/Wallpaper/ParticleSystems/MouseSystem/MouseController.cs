using System.Numerics;

public class MouseController
{
    private readonly ParticleBuffers ParticleSystem;
    private Vector2 MousePos0;
    private Vector2 MousePos1;
    private Vector2 MousePos2;
    private Vector2 MousePos3;
    private float AccumulatedPhase;
    private float SmoothedSpeed;

    static Vector2 CatmullA(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        => 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);

    static Vector2 CatmullB(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        => 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);

    static Vector2 CatmullC(Vector2 p0, Vector2 p1, Vector2 p2)
        => 0.5f * (-p0 + p2);
    static float ApproxCubicLength(Vector2 a, Vector2 b, Vector2 c, Vector2 d, int steps = 8)
    {
        float len = 0f;
        Vector2 prev = d;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 p = ((a * t + b) * t + c) * t + d;
            len += Vector2.Distance(prev, p);
            prev = p;
        }

        return len;
    }
    public MouseController(ParticleBuffers partcileSystem)
    {
        ParticleSystem = partcileSystem;
    }

    public void UpdateStaticResource(ref MouseConstants constant, FrameMetric metric, SystemSettings systemSettings)
    {
        constant.ParticleCount = ParticleSystem.particleCount;
        constant.VelocityBlend = (float)Math.Exp(-systemSettings.mouseSettings.VelocityFallof * metric.DeltaTime);
        constant.mouseSettings = systemSettings.mouseSettings;
        
        Win32.GetCursorPos(out Win32.POINT point);
        float ratio = (float)metric.height/metric.width;
        MousePos3 = MousePos2;
        MousePos2 = MousePos1;
        MousePos1 = MousePos0;
        MousePos0 = new Vector2(((float)point.X) / metric.height, (metric.height - (float)point.Y) / metric.height) * 2 - new Vector2(1/ratio, 1);

        constant.CatmulA = CatmullA(MousePos3, MousePos2, MousePos1, MousePos0);
        constant.CatmulB = CatmullB(MousePos3, MousePos2, MousePos1, MousePos0);
        constant.CatmulC = CatmullC(MousePos3, MousePos2, MousePos1);
        constant.CatmulD = MousePos2;
        constant.DistanceP1P2 = ApproxCubicLength(
            constant.CatmulA,
            constant.CatmulB,
            constant.CatmulC,
            constant.CatmulD
        );
        float length = systemSettings.mouseSettings.WaveLength;
        constant.MousePos = MousePos0;
        AccumulatedPhase += constant.DistanceP1P2;
        AccumulatedPhase %= (float)(length * Math.PI * 2);
        constant.PhaseShift = AccumulatedPhase / length;
        constant.WaveCyclesOnSegment = constant.DistanceP1P2 / length;

        
        float rawSpeed = constant.DistanceP1P2 / MathF.Max(metric.DeltaTime, 0.0001f);

        float blend = MathF.Exp(-10 * metric.DeltaTime);

        SmoothedSpeed =
            rawSpeed + (SmoothedSpeed - rawSpeed) * blend;
        constant.MouseSpeed = SmoothedSpeed;
    }
}