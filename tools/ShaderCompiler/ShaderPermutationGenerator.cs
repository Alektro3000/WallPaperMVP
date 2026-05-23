
using System.Reflection;
using ShaderConventions;
using Vortice.Dxc;

public static class ShaderPermutationGenerator
{
    public static List<PermutationKey> GenerateAllPermutations()
    {
        PropertyInfo[] boolProperties = typeof(PermutationKey)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(bool))
            .ToArray();

        int permutationCount = 1 << boolProperties.Length;
        List<PermutationKey> result = new(permutationCount);

        for (int mask = 0; mask < permutationCount; mask++)
        {
            object key = new PermutationKey();

            for (int i = 0; i < boolProperties.Length; i++)
            {
                bool value = (mask & (1 << i)) != 0;
                boolProperties[i].SetValue(key, value);
            }

            result.Add((PermutationKey)key);
        }

        return result;
    }

    public static DxcDefine[] GetDefines(PermutationKey key)
    {
        List<DxcDefine> defines = new();

        PropertyInfo[] boolProperties = typeof(PermutationKey)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(bool))
            .ToArray();


        object boxedKey = key;

        foreach (PropertyInfo property in boolProperties)
        {
            bool enabled = (bool)property.GetValue(boxedKey)!;

            if (!enabled)
                continue;

            defines.Add(new DxcDefine
            {
                Name = property.Name.ToUpperInvariant(),
                Value = "1"
            });
        }

        return defines.ToArray();
    }
}
