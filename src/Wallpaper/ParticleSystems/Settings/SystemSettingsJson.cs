
using System.Text.Json;
using System.Text.Json.Serialization;

public static class SystemSettingsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters =
            {
                new JsonStringEnumConverter()
            }
    };

    public static void Save(string path, SystemSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, Options);
        File.WriteAllText(path, json);
    }

    public static SystemSettings LoadOrDefault(string path, SystemSettings fallback)
    {
        if (!File.Exists(path))
            return fallback;

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<SystemSettings>(json, Options)
            ?? fallback;
    }
}
