using System.Reflection;
using System.Text.Json.Serialization;
using Particles.Core;
using Vortice.DXGI;

namespace Particles.Settings;
public class SystemSettings
{

    [JsonInclude]
    [JsonConverter(typeof(TypeObjectDictionaryConverter))]
    Dictionary<Type, Object> data;
    public SystemSettings()
    {
        data = ParticleSystemReflection.GetParticleSystemsSettings();
    }

    public SystemSettings Clone()
    {
        return new SystemSettings(data);
    }

    protected SystemSettings(Dictionary<Type, Object> data)
    {
        this.data = new Dictionary<Type, object>(data);
    }
    
    public void UploadSettings<T>(T newValue) where T : notnull
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