namespace ShaderConventions;

public record struct PermutationKey(bool Skeletal)
{
    public static PermutationKey Default = new PermutationKey(false); 
    public override int GetHashCode()
    {
        return Skeletal ? 1 : 0;
    }

    public string GetFileName(string outputBase, string stageSuffix)
    {
        return $"{outputBase}{GetHashCode():X}.{stageSuffix.ToLowerInvariant()}.cso";
    }
}
