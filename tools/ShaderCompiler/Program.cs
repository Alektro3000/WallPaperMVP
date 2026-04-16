
using System.Text.RegularExpressions;
using Vortice.Dxc;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Run(args);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ShaderCompiler failed:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Run(string[] args)
    {
        if (args.Length < 3)
           throw new ArgumentException("Expected: <assemblyPath> <projectDir> <outputDir>");

        string assemblyPath = Path.GetFullPath(args[0]);
        string projectDir = Path.GetFullPath(args[1]);
        string outputDir = Path.GetFullPath(args[2]);
        
        // string assemblyPath = "G:\\projects\\mine\\WallPaperMVP\\src\\Wallpaper\\bin\\Debug\\net10.0-windows\\WallpaperMVP.dll" ;
        // string projectDir = "G:\\projects\\mine\\WallPaperMVP\\src\\Wallpaper" ;
        // string outputDir = "G:\\projects\\mine\\WallPaperMVP\\src\\Wallpaper\\bin\\Debug\\net10.0-windows\\" ;

        Console.WriteLine($"assemblyPath = {assemblyPath}");

        var runtimeAssemblies = Directory.GetFiles(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!,
            "*.dll");

        var appAssemblies = Directory.GetFiles(
            Path.GetDirectoryName(assemblyPath)!,
            "*.dll");

        var resolver = new PathAssemblyResolver(runtimeAssemblies.Concat(appAssemblies));

        using var mlc = new MetadataLoadContext(resolver);

        var assembly = mlc.LoadFromAssemblyPath(assemblyPath);

        string inputRoot = Path.Combine(projectDir, "shaders");
        string outputRoot = Path.Combine(outputDir, "shaders");


        foreach (var type in assembly.GetTypes())
        {
            foreach (var attrData in type.GetCustomAttributesData())
            {
                if (attrData.AttributeType.FullName != "ShaderAttribute" &&
                    attrData.AttributeType.FullName != "ShaderAttribute")
                    continue;

                string path = (string)attrData.ConstructorArguments[0].Value!;
                string stage = (string)attrData.ConstructorArguments[1].Value!;
                string inputPath = Path.Combine(inputRoot, path);
                string outputPath = Path.Combine(outputRoot,
                    Path.ChangeExtension(path,".cso"));

                Console.WriteLine($"{type.FullName}: {path} [{stage}]");
                Console.WriteLine($"{inputPath} to {outputPath}");

                //if(File.GetLastWriteTimeUtc(inputPath) >= File.GetLastWriteTimeUtc(outputPath))
                    Compile(inputPath, inputRoot, outputPath, stage);
            }
        }
    }
    private static void Compile(string inputPath, string inputRoot, string outputPath, string Stage)
    {
        
        DxcShaderStage stage = Stage switch
        {
            "vs" => DxcShaderStage.Vertex,
            "ps" => DxcShaderStage.Pixel,
            "cs" => DxcShaderStage.Compute,
            "gs" => DxcShaderStage.Geometry,
            _ => throw new InvalidOperationException($"Unknown shader stage: {Stage}")
        };

        var bytecode = ShaderCompiler.PreCompile(inputPath, inputRoot, stage);

        string? directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(outputPath, bytecode.ToArray());
    }
}

class ShaderCompiler
{
    private ShaderCompiler(){}
    public static DxcCompilerOptions GetOptions()
    {
        return new DxcCompilerOptions
        {
            ShaderModel = DxcShaderModel.Model6_0,
            OptimizationLevel = 3,  // -O3 optimization
            EnableDebugInfo = false
        };
    }
    public static ReadOnlyMemory<byte> PreCompile(string path, string root, DxcShaderStage stage)
    {
        string fullPath = path;

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }


        string source = LoadShaderWithIncludes(fullPath, root, new HashSet<string>());

        //string source = File.ReadAllText(fullPath);

        Console.WriteLine($"Compiling {path} as {stage}...");

        var result = DxcCompiler.Compile(stage, 
            source, 
            "main", 
            GetOptions());

        // IMPORTANT: Always check for errors first
        try
        {
            var errorBlob = result.GetOutput(DxcOutKind.Errors);
            if (errorBlob != null && errorBlob.AsBytes().Length > 0)
            {
                string errors = System.Text.Encoding.UTF8.GetString(errorBlob.AsBytes());
                Console.WriteLine($"ERRORS:\n{errors}");
                throw new InvalidOperationException($"Shader compilation failed for {path}:\n{errors}");
            }
        }
        catch (SharpGen.Runtime.SharpGenException ex)
        {
            Console.WriteLine($"No error blob available: {ex.Message}");
        }

        // Now get the compiled bytecode
        try
        {
            return result.GetObjectBytecodeMemory();
        }
        catch (SharpGen.Runtime.SharpGenException ex)
        {
            // Try to get detailed error information
            string errorDetails = "";
            try
            {
                var errorBlob = result.GetOutput(DxcOutKind.Errors);
                if (errorBlob != null && errorBlob.AsBytes().Length > 0)
                {
                    errorDetails = "\n\nDetailed errors:\n" + System.Text.Encoding.UTF8.GetString(errorBlob.AsBytes());
                }
            }
            catch { }

            throw new InvalidOperationException(
                $"Failed to compile {path}.\n" +
                $"Stage: {stage}\n" +
                $"Entry point: 'main'\n" +
                $"Please check:\n" +
                $"1. The shader syntax is correct for the target stage\n" +
                $"2. Pixel shaders must have SV_TARGET output semantic\n" +
                $"3. Vertex shaders must output SV_POSITION\n" +
                errorDetails,
                ex
            );
        }
    }
    
    private static string LoadShaderWithIncludes(
        string filePath,
        string shadersRoot,
        HashSet<string> includeStack)
    {
        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Included shader file not found: {filePath}");

        if (includeStack.Contains(filePath))
            throw new InvalidOperationException($"Circular include detected: {filePath}");

        includeStack.Add(filePath);

        string source = File.ReadAllText(filePath);
        string currentDir = Path.GetDirectoryName(filePath)!;

        var includeRegex = new Regex(@"^\s*#include\s+""([^""]+)""", RegexOptions.Multiline);
        string resolved = includeRegex.Replace(source, match =>
        {
            string relativeInclude = match.Groups[1].Value;

            string includePath = Path.GetFullPath(Path.Combine(currentDir, relativeInclude));

            if (!includePath.StartsWith(Path.GetFullPath(shadersRoot), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Include escapes shader root: {relativeInclude}");

            return LoadShaderWithIncludes(includePath, shadersRoot, includeStack);
        });

        includeStack.Remove(filePath);
        return resolved;
    }
}
