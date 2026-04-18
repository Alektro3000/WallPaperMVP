public class SystemSettings
{
    public MouseSettings mouseSettings = new MouseSettings(0.016f);
    public TextSettings textSettings = new TextSettings();
    public WhirlSettings whirlSettings = new WhirlSettings();

    public StripSettings stripSettings = new StripSettings();
    public CornerSettings cornerSettings = new CornerSettings();
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