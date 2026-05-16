using System.Text.RegularExpressions;
using Serilog;
using Vortice.Dxc;

internal static class Program
{
    private static readonly Dictionary<string, DxcShaderStage> StageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VS"] = DxcShaderStage.Vertex,
        ["PS"] = DxcShaderStage.Pixel,
        ["CS"] = DxcShaderStage.Compute,
        ["GS"] = DxcShaderStage.Geometry,
        ["HS"] = DxcShaderStage.Hull,
        ["DS"] = DxcShaderStage.Domain,
        ["MS"] = DxcShaderStage.Mesh,
        ["AS"] = DxcShaderStage.Amplification,
    };

    private static readonly Regex EntryRegex = new(@"\bMAIN_(VS|PS|CS|GS|HS|DS|MS|AS)\b", RegexOptions.Compiled);

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
        if (args.Length < 2)
            throw new ArgumentException("Expected: <projectDir> <outputDir>");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        string projectDir = Path.GetFullPath(args[0]);
        string outputDir = Path.GetFullPath(args[1]);
        string inputRoot = Path.Combine(projectDir, "shaders");
        string outputRoot = Path.Combine(outputDir, "shaders");

        if (!Directory.Exists(inputRoot))
            throw new DirectoryNotFoundException($"Shader source root not found: {inputRoot}");

        bool failed = false;
        var shaderFiles = Directory.GetFiles(inputRoot, "*.hlsl", SearchOption.AllDirectories);

        foreach (var inputPath in shaderFiles)
        {
            try
            {
                CompileShaderFile(inputPath, inputRoot, outputRoot);
            }
            catch (Exception ex)
            {
                failed = true;
                Log.Error("Failed to compile shader file {ShaderPath}", inputPath);
                Log.Error("Error message {Message}", ex.Message);
            }
        }

        Log.CloseAndFlush();
        if (failed)
            throw new Exception("Failed to Compile Shaders");
    }

    private static void CompileShaderFile(string inputPath, string inputRoot, string outputRoot)
    {
        var loadResult = ShaderCompiler.LoadShaderWithIncludes(inputPath, inputRoot, new HashSet<string>());
        var stageMatches = EntryRegex.Matches(loadResult.Source);
        if (stageMatches.Count == 0)
        {
            throw new InvalidOperationException(
                $"Shader file must contain at least one MAIN_<STAGE> entry point: {inputPath}");
        }

        var foundStages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in stageMatches)
        {
            string stageName = match.Groups[1].Value;
            if (!foundStages.Add(stageName))
            {
                throw new InvalidOperationException(
                    $"Shader file contains duplicate MAIN_{stageName} entry point: {inputPath}");
            }
        }

        string relativePath = Path.GetRelativePath(inputRoot, inputPath);
        string relativeBase = Path.ChangeExtension(relativePath, null)!;
        string outputBase = Path.Combine(outputRoot, relativeBase);

        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string stageName in foundStages)
        {
            string stageSuffix = stageName.ToLowerInvariant();
            outputs[stageName] = $"{outputBase}.{stageSuffix}.cso";
        }

        var dependencyNewestTime = loadResult.Dependencies
            .Max(File.GetLastWriteTimeUtc);

        bool isUpToDate = outputs.Count > 0 && outputs.Values.All(File.Exists);
        if (isUpToDate)
        {
            DateTime oldestOutput = outputs.Values.Min(File.GetLastWriteTimeUtc);
            if(oldestOutput >= dependencyNewestTime)
            {
                Log.Information("Skip up-to-date shader file {ShaderPath}", relativePath);
                Log.Debug("dependencyNewestTime is {dependencyNewestTime},  oldestOutput is {oldestOutput}", dependencyNewestTime, oldestOutput);
                return;
            }
        }

        foreach (string stageName in foundStages)
        {
            var stage = StageMap[stageName];
            string stageSuffix = stageName.ToLowerInvariant();
            string outputPath = outputs[stageName];

            Log.Information("{ShaderPath}: MAIN_{Stage} [{StageSuffix}]",
                relativePath, stageName, stageSuffix);
            Log.Information("{InputPath} to {OutputPath}", inputPath, outputPath);

            var bytecode = ShaderCompiler.PreCompile(inputPath, stage, $"MAIN_{stageName}", loadResult.Source);
            string? directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(outputPath, bytecode.ToArray());
        }
    }
}

class ShaderCompiler
{
    public readonly record struct LoadResult(string Source, HashSet<string> Dependencies);

    private ShaderCompiler() { }

    public static DxcCompilerOptions GetOptions()
    {
        return new DxcCompilerOptions
        {
            ShaderModel = DxcShaderModel.Model6_0,
            OptimizationLevel = 3,
            EnableDebugInfo = false,
        };
    }

    public static ReadOnlyMemory<byte> PreCompile(string path, DxcShaderStage stage, string entryPoint, string source)
    {
        string fullPath = path;
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }

        Console.WriteLine($"Compiling {path} as {stage} ({entryPoint})...");

        var result = DxcCompiler.Compile(stage, source, entryPoint, GetOptions());

        try
        {
            var errorBlob = result.GetOutput(DxcOutKind.Errors);
            if (errorBlob != null && errorBlob.AsBytes().Length > 0)
            {
                string errors = System.Text.Encoding.UTF8.GetString(errorBlob.AsBytes());
                throw new InvalidOperationException($"Shader compilation failed for {path}:\n{errors}");
            }
        }
        catch (SharpGen.Runtime.SharpGenException)
        {
            // no error blob
        }

        try
        {
            return result.GetObjectBytecodeMemory();
        }
        catch (SharpGen.Runtime.SharpGenException ex)
        {
            throw new InvalidOperationException(
                $"Failed to compile {path}. Stage: {stage}, Entry point: {entryPoint}",
                ex);
        }
    }

    public static LoadResult LoadShaderWithIncludes(
        string filePath,
        string shadersRoot,
        HashSet<string> includeStack,
        HashSet<string>? dependencies = null)
    {
        dependencies ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Included shader file not found: {filePath}");

        if (includeStack.Contains(filePath))
            throw new InvalidOperationException($"Circular include detected: {filePath}");

        includeStack.Add(filePath);
        dependencies.Add(filePath);

        string source = File.ReadAllText(filePath);
        string currentDir = Path.GetDirectoryName(filePath)!;

        var includeRegex = new Regex(@"^\s*#include\s+""([^""]+)""", RegexOptions.Multiline);
        string resolved = includeRegex.Replace(source, match =>
        {
            string relativeInclude = match.Groups[1].Value;
            string includePath = Path.GetFullPath(Path.Combine(currentDir, relativeInclude));

            if (!includePath.StartsWith(Path.GetFullPath(shadersRoot), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Include escapes shader root: {relativeInclude}");

            return LoadShaderWithIncludes(includePath, shadersRoot, includeStack, dependencies).Source;
        });

        includeStack.Remove(filePath);
        return new LoadResult(resolved, dependencies);
    }
}
