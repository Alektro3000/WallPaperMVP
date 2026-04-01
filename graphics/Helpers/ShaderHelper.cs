
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Dxc;
using static Vortice.Direct3D12.D3D12;

class ShaderHelper
{
    private ShaderHelper(){}
    public static DxcCompilerOptions GetOptions()
    {
        return new DxcCompilerOptions
        {
            ShaderModel = DxcShaderModel.Model6_0,
            OptimizationLevel = 3,  // -O3 optimization
            EnableDebugInfo = false
        };
    }
    
    public static ReadOnlyMemory<byte> PreCompile(string path, DxcShaderStage stage)
    {
        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shaders", path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }

        string source = File.ReadAllText(fullPath);

        Console.WriteLine($"Compiling {path} as {stage}...");

        var result = DxcCompiler.Compile(stage, source, "main", GetOptions());

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
    
}