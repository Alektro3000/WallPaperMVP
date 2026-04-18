[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UiRangeAttribute : Attribute
{
    public float Min { get; }
    public float Max { get; }
    public float Step { get; }

    public UiRangeAttribute(float min, float max, float step = 0.01f)
    {
        Min = min;
        Max = max;
        Step = step;
    }
}