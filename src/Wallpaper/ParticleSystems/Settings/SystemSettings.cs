public class SystemSettings
{
    public MouseSystem.Settings mouseSettings = new MouseSystem.Settings();
    public TextSettings textSettings = new TextSettings();
    public WhirlSettings whirlSettings = new WhirlSettings();

    public StripSettings stripSettings = new StripSettings();
    public CornerSystem.Settings cornerSettings = new CornerSystem.Settings();
    public SystemSettings Clone()
    {
        return new SystemSettings
        {
            mouseSettings = this.mouseSettings,
            textSettings = this.textSettings,
            whirlSettings = this.whirlSettings,
            stripSettings = this.stripSettings,
            cornerSettings = this.cornerSettings
        };
    }
}