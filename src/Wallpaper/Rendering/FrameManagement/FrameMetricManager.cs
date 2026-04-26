
using System.Diagnostics;
using System.Numerics;

namespace Renderer.FrameManagement;

//Class To Generate Frame Metric 
public class FrameMetricManager
{
    Stopwatch timer = Stopwatch.StartNew();
    private double previousTime;
    private uint FrameIndex;
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
        (
            (float)(currentTime - previousTime),
            FrameIndex,
            width,
            height
        );
        previousTime = currentTime;
        return ans;
    }

}