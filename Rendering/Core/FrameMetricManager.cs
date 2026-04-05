
using System.Diagnostics;

    public struct FrameMetric {
        public float DeltaTime;
        public uint FrameIndex;
        public int width;
        public int height;
    }
public class FrameMetricManager
{
    Stopwatch timer = Stopwatch.StartNew();
    private double previousTime;
    private uint FrameIndex;
    private bool StopwatchLaunched;
    private int width;
    private int height;
    public FrameMetricManager(int width, int height)
    {
        this.width = width;
        this.height = height;
        previousTime = timer.Elapsed.TotalSeconds;
        FrameIndex = 0;
    }
    public FrameMetric Update()
    {
        FrameIndex++;
        var currentTime = timer.Elapsed.TotalSeconds;
        var ans = new FrameMetric
        {
            FrameIndex = FrameIndex,
            DeltaTime = (float)(currentTime - previousTime),
            width = width,
            height = height,
        };
        previousTime = currentTime;
        return ans;
    }

}