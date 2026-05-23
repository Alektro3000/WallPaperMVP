

using Models;
using Models.Material;
using SharpGLTF.Schema2;

public class PrimitiveLoaderRegistry(InitContext initContext, BindlessTextureProvider bindlessTextureProvider)
{    
    PrimitiveLoader skeletalLoader = new SkeletalPrimitiveLoader(initContext, bindlessTextureProvider);
    PrimitiveLoader staticLoader = new StaticPrimitiveLoader(initContext, bindlessTextureProvider);

    public PrimitiveLoader GetPrimitiveLoader(MeshPrimitive primitive)
    {
        bool isSkeletal = primitive.GetVertexAccessor("JOINTS_0") != null
            && primitive.GetVertexAccessor("WEIGHTS_0") != null;

        return isSkeletal ? skeletalLoader : staticLoader;
    }

    public List<RootSignatureDefinition> GetRootSignatureDefinitions()
    {
        return [skeletalLoader.GetRootSignatureDefinition(),staticLoader.GetRootSignatureDefinition()];
    }
    public List<MaterialDefinition> GetMaterialDefinition()
    {
        return [skeletalLoader.GetMaterialDefinition(),staticLoader.GetMaterialDefinition()];
    }
}