
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
    public float Smoothed = -1;

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

        float CalculatedDelta = (float)(currentTime - previousTime);
        
        if(Smoothed < 0)
        {
            Smoothed = CalculatedDelta;
        }
        else
        {
            Smoothed = Smoothed * 0.98f + CalculatedDelta * 0.02f;
        }
        
        var ans = new FrameMetric
        (
            CalculatedDelta,
            FrameIndex,
            width,
            height,
            Smoothed
        );
        previousTime = currentTime;
        return ans;
    }

}