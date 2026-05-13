using System.Numerics;

namespace Renderer.FrameManagement;

//Struct to pass basic info about metric
public struct FrameMetric
{
    public readonly float DeltaTime;
    public readonly uint FrameIndex;
    public readonly int width;
    public readonly int height;
    public readonly float SmoothedDeltaTime;
    public Vector2 size
    {
        get => new Vector2(width, height);
    }
    public float ratio
    {
        get => (float)width/height;
    }
    public FrameMetric(
        float DeltaTime,
        uint FrameIndex,
        int width,
        int height,
        float SmoothedDeltaTime
    )
    {
        this.DeltaTime = DeltaTime;
        this.FrameIndex = FrameIndex;
        this.width = width;
        this.height = height;
        this.SmoothedDeltaTime = SmoothedDeltaTime;
    }
}