public class SettingsForm : Form
{
    public Button CloseButton = new Button
        {
            Text = "Hello settings",
            Dock = DockStyle.Fill
        };
    public bool ShouldBeClosed = false;
    public SettingsForm(Renderer renderer)
    {
        Text = "Wallpaper Settings";
        Width = 400;
        Height = 300;

        Controls.Add(CloseButton);
        CloseButton.Click += OnClick;
    }

    public void OnClick(object? obj, EventArgs eventArgs)
    {
        ShouldBeClosed = true;
        Close();
    }
}