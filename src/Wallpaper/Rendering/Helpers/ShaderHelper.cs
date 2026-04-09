

class ShaderHelper
{
    private ShaderHelper(){}
    public static ReadOnlyMemory<byte> GetShader(string path)
    {
        string shadersRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shaders");
        string fullPath = Path.ChangeExtension(Path.Combine(shadersRoot, path),".cso");

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }

        return File.ReadAllBytes(fullPath);
    }
}