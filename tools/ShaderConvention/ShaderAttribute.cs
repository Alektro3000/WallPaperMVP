
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ShaderAttribute : Attribute
{
    public string File { get; }
    public string Stage { get; }

    public string EntryPoint { get; }    
    public string? OutputName { get; }

    public ShaderAttribute(string file, string stage, string entryPoint = "main", string? outputName = null)
    {
        File = file;
        Stage = stage;
        EntryPoint = entryPoint;
        OutputName = outputName;
    }
}