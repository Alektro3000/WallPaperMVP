
namespace ParticleSystems;
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UiLabelAttribute : Attribute
{
    public string Label { get; }

    public UiLabelAttribute(string label)
    {
        Label = label;
    }
}