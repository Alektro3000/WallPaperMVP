
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace Particles.Settings;

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

        try
        {
            var json = File.ReadAllText(path);

            var loaded = JsonSerializer.Deserialize<SystemSettings>(json, Options);

            if (loaded == null)
                return fallback;

            loaded.ApplyFallback(fallback);
            return loaded;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load settings from {Path}, returning fallback", path);
            return fallback;
        }
    }
}


public sealed class TypeObjectDictionaryConverter
    : JsonConverter<Dictionary<Type, object>>
{
    public override void Write(
        Utf8JsonWriter writer,
        Dictionary<Type, object> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var (type, obj) in value)
        {
            writer.WritePropertyName(type.FullName!);

            JsonSerializer.Serialize(
                writer,
                obj,
                obj.GetType(),
                options);
        }

        writer.WriteEndObject();
    }
    public override Dictionary<Type, object> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var result = new Dictionary<Type, object>();

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return result;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name.");

            var typeName = reader.GetString();
            reader.Read();

            try
            {
                var element = JsonElement.ParseValue(ref reader);

                var settingsType = FindTypeByFullName(typeName!)
                    ?? throw new JsonException($"Type not found: {typeName}");

                var value = element.Deserialize(settingsType, options);

                if (value != null)
                    result[settingsType] = value;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse settings entry for type {TypeName}", typeName);
            }
        }

        throw new JsonException("Unexpected EOF in TypeObjectDictionaryConverter");
    }

    private static Type? FindTypeByFullName(string fullName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(a => a.GetType(fullName, throwOnError: false))
            .FirstOrDefault(t => t != null);
    }
}