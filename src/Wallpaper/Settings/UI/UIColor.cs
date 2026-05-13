
namespace Settings;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UiColorAttribute : Attribute
{
    // If true, interpret Vector3/Vector4 as 0..1 floats.
    // Otherwise UI may treat it as 0..255.
    public bool Normalized { get; }

    // Optional alpha channel support.
    public bool HasAlpha { get; }

    public UiColorAttribute(bool normalized = true, bool hasAlpha = false)
    {
        Normalized = normalized;
        HasAlpha = hasAlpha;
    }
}