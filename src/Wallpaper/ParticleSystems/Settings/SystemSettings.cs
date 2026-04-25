public class SystemSettings
{
    public MouseSystem.Settings mouseSettings = new MouseSystem.Settings();
    public TextSystem.Settings textSettings = new TextSystem.Settings();
    public WhirlSystem.Settings whirlSettings = new WhirlSystem.Settings();

    public StripSystem.Settings stripSettings = new StripSystem.Settings();
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