using System.Reflection.Metadata;

namespace ShaderConventions;

public record struct PermutationKey(bool Skeletal, bool DepthPass = false)
{
    public static PermutationKey Default = new PermutationKey(false, false);
    public override int GetHashCode()
    {
        return
            (Skeletal ? 1 : 0) |
            (DepthPass ? 2 : 0);
    }

    public string GetFileName(string outputBase, string stageSuffix)
    {
        return $"{outputBase}{GetHashCode():X}.{stageSuffix.ToLowerInvariant()}.cso";
    }
}
