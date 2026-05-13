namespace Settings;
public sealed class SettingsStore
{
    private readonly Lock _lock = new();
    private SystemSettings _settings;
    private readonly string _path;

    public SettingsStore(string path, SystemSettings defaults)
    {
        _path = path;
        _settings = SystemSettingsJson.LoadOrDefault(path, defaults);
    }
    
    public SystemSettings GetSnapshot()
    {
        lock (_lock)
        {
            return _settings.Clone();
        }
    }

    public void Update(Action<SystemSettings> update)
    {
        lock (_lock)
        {
            update(_settings);
        }
    }

    public void Save()
    {
        SystemSettingsJson.Save(_path, GetSnapshot());
    }

    public void Load()
    {
        lock (_lock)
            _settings = SystemSettingsJson.LoadOrDefault(_path, _settings);
    }
}