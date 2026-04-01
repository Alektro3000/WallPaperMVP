public class SettingsForm : Form
{
    public SettingsForm()
    {
        Text = "Wallpaper Settings";
        Width = 400;
        Height = 300;

        Controls.Add(new Label
        {
            Text = "Hello settings",
            Dock = DockStyle.Fill
        });
    }
}