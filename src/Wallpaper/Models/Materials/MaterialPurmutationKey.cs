
using ShaderConventions;

public record struct MaterialPermutationKey(PermutationKey ShaderPermutation, bool TwoSided)
{
    public MaterialPermutationKey withDepthPass(bool depthPass)
    {
        return this with { ShaderPermutation = (ShaderPermutation with {DepthPass = depthPass}) };
    }
}