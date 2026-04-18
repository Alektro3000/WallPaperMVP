[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UiVector2Attribute : Attribute
{
    public float MinX { get; }
    public float MaxX { get; }
    public float StepX { get; }

    public float MinY { get; }
    public float MaxY { get; }
    public float StepY { get; }

    // Optional labels for UI
    public string XLabel { get; }
    public string YLabel { get; }

    public UiVector2Attribute(
        float minX,
        float maxX,
        float stepX,
        float minY,
        float maxY,
        float stepY,
        string xLabel = "X",
        string yLabel = "Y")
    {
        MinX = minX;
        MaxX = maxX;
        StepX = stepX;

        MinY = minY;
        MaxY = maxY;
        StepY = stepY;

        XLabel = xLabel;
        YLabel = yLabel;
    }
}