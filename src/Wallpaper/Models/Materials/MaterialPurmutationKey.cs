
using ShaderConventions;

namespace Models;
public record struct MaterialPermutationKey(PermutationKey ShaderPermutation, bool TwoSided, AlphaMode AlphaMode)
{
    public MaterialPermutationKey withDepthPass(bool depthPass)
    {
        return this with { ShaderPermutation = (ShaderPermutation with {DepthPass = depthPass}) };
    }
}