using System.Numerics;
using System.Windows.Forms;

public sealed class SettingsForm : Form
{
    private readonly SystemSettings _settings;
    public bool ShouldBeClosed = false;

    public SettingsForm(SystemSettings settings)
    {
        _settings = settings;

        Text = "Particle Settings";
        Width = 420;
        Height = 700;

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        tabs.TabPages.Add(CreateMouseTab());
        tabs.TabPages.Add(CreateTextTab());
        tabs.TabPages.Add(CreateWhirlTab());


        Controls.Add(tabs);
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };

        var closeAppButton = new Button
        {
            Text = "Close App",
            Width = 100,
            Height = 30
        };
        closeAppButton.Click += (_, _) =>
        {
            ShouldBeClosed = true;
            Hide();
        };

        var hideButton = new Button
        {
            Text = "Hide",
            Width = 100,
            Height = 30
        };
        hideButton.Click += (_, _) => Hide();

        buttons.Controls.Add(closeAppButton);
        buttons.Controls.Add(hideButton);

        FormClosing += OnFormClosing;
        Controls.Add(buttons);

    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // User clicked the X button
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private TabPage CreateMouseTab()
    {
        var tab = new TabPage("Mouse");
        var panel = CreatePanel(tab);

        AddFloat(panel, "Color R", _settings.mouseSettings.Color.X,
            v => _settings.mouseSettings.Color.X = v);
        AddFloat(panel, "Color G", _settings.mouseSettings.Color.Y,
            v => _settings.mouseSettings.Color.Y = v);
        AddFloat(panel, "Color B", _settings.mouseSettings.Color.Z,
            v => _settings.mouseSettings.Color.Z = v);

        AddFloat(panel, "Size", _settings.mouseSettings.Size,
            v => _settings.mouseSettings.Size = v);

        AddFloat(panel, "Grid Size X", _settings.mouseSettings.GridSize.X,
            v => _settings.mouseSettings.GridSize.X = v);
        AddFloat(panel, "Grid Size Y", _settings.mouseSettings.GridSize.Y,
            v => _settings.mouseSettings.GridSize.Y = v);

        AddFloat(panel, "Velocity", _settings.mouseSettings.Velocity,
            v => _settings.mouseSettings.Velocity = v);

        AddFloat(panel, "Life Time", _settings.mouseSettings.LifeTime,
            v => _settings.mouseSettings.LifeTime = v);

        AddFloat(panel, "Spawn Rate", _settings.mouseSettings.SpawnRate,
            v => _settings.mouseSettings.SpawnRate = v);

        AddFloat(panel, "Spawn Rate / Unit", _settings.mouseSettings.SpawnRatePerUnit,
            v => _settings.mouseSettings.SpawnRatePerUnit = v);

        AddFloat(panel, "Init Speed", _settings.mouseSettings.InitSpeed,
            v => _settings.mouseSettings.InitSpeed = v);

        return tab;
    }

    private TabPage CreateTextTab()
    {
        var tab = new TabPage("Text");
        var panel = CreatePanel(tab);

        AddFloat(panel, "Life Time", _settings.textSettings.LifeTime,
            v => _settings.textSettings.LifeTime = v);

        AddFloat(panel, "Spawn Rate", _settings.textSettings.SpawnRate,
            v => _settings.textSettings.SpawnRate = v);

        AddFloat(panel, "Size", _settings.textSettings.Size,
            v => _settings.textSettings.Size = v);

        AddFloat(panel, "Speed", _settings.textSettings.Speed,
            v => _settings.textSettings.Speed = v);

        AddFloat(panel, "Init Region", _settings.textSettings.InitRegion,
            v => _settings.textSettings.InitRegion = v);

        AddFloat(panel, "Init Offset", _settings.textSettings.InitOffset,
            v => _settings.textSettings.InitOffset = v);

        return tab;
    }

    private TabPage CreateWhirlTab()
    {
        var tab = new TabPage("Whirl");
        var panel = CreatePanel(tab);

        AddFloat(panel, "Center X", _settings.whirlSettings.CenterPosition.X,
            v => _settings.whirlSettings.CenterPosition.X = v);
        AddFloat(panel, "Center Y", _settings.whirlSettings.CenterPosition.Y,
            v => _settings.whirlSettings.CenterPosition.Y = v);

        AddFloat(panel, "Life Time", _settings.whirlSettings.LifeTime,
            v => _settings.whirlSettings.LifeTime = v);

        AddFloat(panel, "Spawn Rate", _settings.whirlSettings.SpawnRate,
            v => _settings.whirlSettings.SpawnRate = v);

        AddFloat(panel, "Speed", _settings.whirlSettings.Speed,
            v => _settings.whirlSettings.Speed = v);

        AddFloat(panel, "Tangent", _settings.whirlSettings.Tangent,
            v => _settings.whirlSettings.Tangent = v);

        AddFloat(panel, "Radial", _settings.whirlSettings.Radial,
            v => _settings.whirlSettings.Radial = v);

        AddFloat(panel, "Size", _settings.whirlSettings.Size,
            v => _settings.whirlSettings.Size = v);

        AddFloat(panel, "Init Region", _settings.whirlSettings.InitRegion,
            v => _settings.whirlSettings.InitRegion = v);

        AddFloat(panel, "Init Offset", _settings.whirlSettings.InitOffset,
            v => _settings.whirlSettings.InitOffset = v);

        return tab;
    }

    private static FlowLayoutPanel CreatePanel(TabPage page)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8)
        };

        page.Controls.Add(panel);
        return panel;
    }

    private static void AddFloat(FlowLayoutPanel panel, string label, float value, Action<float> setter)
    {
        var row = new Panel
        {
            Width = 340,
            Height = 32
        };

        var text = new Label
        {
            Text = label,
            Left = 0,
            Top = 8,
            Width = 150
        };

        var box = new NumericUpDown
        {
            Left = 160,
            Width = 120,
            DecimalPlaces = 4,
            Increment = 0.01m,
            Minimum = -100000,
            Maximum = 100000,
            Value = (decimal)value
        };

        box.ValueChanged += (_, _) => setter((float)box.Value);

        row.Controls.Add(text);
        row.Controls.Add(box);
        panel.Controls.Add(row);
    }
}
