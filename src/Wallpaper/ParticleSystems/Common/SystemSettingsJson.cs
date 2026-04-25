
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ParticleSystems;

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
                throw new JsonException();

            var typeName = reader.GetString()!;

            var settingsType = FindTypeByFullName(typeName)
                ?? throw new JsonException($"Type not found: {typeName}");

            reader.Read();

            var value = JsonSerializer.Deserialize(
                ref reader,
                settingsType,
                options);

            if (value != null)
                result[settingsType] = value;
        }

        throw new JsonException();
    }

    private static Type? FindTypeByFullName(string fullName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(a => a.GetType(fullName, throwOnError: false))
            .FirstOrDefault(t => t != null);
    }
}