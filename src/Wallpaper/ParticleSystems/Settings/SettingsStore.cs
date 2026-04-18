public sealed class SettingsStore
{
    private readonly object _lock = new();
    private SystemSettings _settings;

    public SettingsStore(SystemSettings initialSettings)
    {
        _settings = initialSettings.Clone();
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
}