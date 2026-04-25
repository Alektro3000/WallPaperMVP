using System.Reflection;
using System.Text.Json.Serialization;
using Vortice.DXGI;

namespace ParticleSystems;
public class SystemSettings
{

    [JsonInclude]
    [JsonConverter(typeof(TypeObjectDictionaryConverter))]
    Dictionary<Type, Object> data;
    public SystemSettings()
    {
        data = ParticleSystemReflection.GetParticleSystemSettings();
    }

    public SystemSettings Clone()
    {
        return new SystemSettings(data);
    }

    protected SystemSettings(Dictionary<Type, Object> data)
    {
        this.data = new Dictionary<Type, object>(data);
    }
    
    public void UploadSettings<T>(T newValue)
    {
        data[typeof(T)] = newValue;
    }

    public T GetSettings<T>()
    {
        return (T)data[typeof(T)];
    }

    public object GetSettings(Type settingsType)
    {
        return data[settingsType];
    }
    public IEnumerable<Type> GetSavedTypes()
    {
        return data.Keys;
    }
}