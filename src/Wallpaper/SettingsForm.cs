using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

public class SettingsForm : Form
{
    private readonly TableLayoutPanel _layout;
    private readonly Button _applyButton;
    private readonly Button _closeButton;

    private readonly Button _colorButton;
    private readonly Panel _colorPreview;
    private readonly ColorDialog _colorDialog;

    private Vector3 _selectedColor;

    private readonly NumericUpDown _size;
    private readonly NumericUpDown _lifeTime;
    private readonly NumericUpDown _spawnRate;
    private readonly NumericUpDown _spawnRatePerUnit;
    private readonly NumericUpDown _velocity;

    public bool ShouldBeClosed = false;

    public MouseSettings Settings { get; private set; }

    public event Action<MouseSettings>? SettingsApplied;

    public SettingsForm(MouseSettings initialSettings)
    {
        Settings = initialSettings;

        Text = "Wallpaper Settings";
        Width = 420;
        Height = 500;
        StartPosition = FormStartPosition.CenterScreen;

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
            Padding = new Padding(12)
        };

        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Controls.Add(_layout);
        _selectedColor = initialSettings.Color;

        _colorDialog = new ColorDialog
        {
            FullOpen = true
        };

        _colorPreview = new Panel
        {
            Width = 48,
            Height = 24,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = VectorToColor(_selectedColor),
            Margin = new Padding(0, 2, 8, 2)
        };

        _colorButton = new Button
        {
            Text = "Choose...",
            AutoSize = true
        };

        _colorButton.Click += (_, _) =>
        {
            _colorDialog.Color = _colorPreview.BackColor;

            if (_colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                _colorPreview.BackColor = _colorDialog.Color;
                _selectedColor = ColorToVector(_colorDialog.Color);
                UpdateSettings();
            }
        };

        var colorPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false
        };

        colorPanel.Controls.Add(_colorPreview);
        colorPanel.Controls.Add(_colorButton);

        AddRow("Color", colorPanel);

        _size = CreateFloatEditor(0, 0.2m, 0.001m, (decimal)initialSettings.Size);
        _lifeTime = CreateFloatEditor(0, 3, 0.1m, (decimal)initialSettings.LifeTime);
        _spawnRate = CreateFloatEditor(0, 100000, 100m, (decimal)initialSettings.SpawnRate);
        _spawnRatePerUnit = CreateFloatEditor(0, 100000, 100m, (decimal)initialSettings.SpawnRatePerUnit);
        _velocity = CreateFloatEditor(0, 10, 0.01m, (decimal)initialSettings.Velocity);
        


        AddRow("Size", _size);
        AddRow("LifeTime", _lifeTime);
        AddRow("SpawnRate", _spawnRate);
        AddRow("Spawn / Unit", _spawnRatePerUnit);
        AddRow("Velocity", _velocity);


        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false
        };

        _applyButton = new Button
        {
            Text = "Close",
            AutoSize = true
        };

        _closeButton = new Button
        {
            Text = "Exit",
            AutoSize = true
        };

        _applyButton.Click += (_, _) => Hide();
        _closeButton.Click += OnCloseClick;

        buttonsPanel.Controls.Add(_closeButton);
        buttonsPanel.Controls.Add(_applyButton);

        AddFullWidthRow(buttonsPanel);
        FormClosing += OnFormClosing;
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
    
    private NumericUpDown CreateFloatEditor(decimal min, decimal max, decimal increment, decimal value)
    {
        var ans = new NumericUpDown
        {
            DecimalPlaces = 3,
            Minimum = min,
            Maximum = max,
            Increment = increment,
            Value = Clamp(value, min, max),
            Dock = DockStyle.Fill,
        };

        ans.ValueChanged += (_, _) => UpdateSettings();
        return ans;
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private Panel CreateFuturePanel(string text)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 28
        };

        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray
        };

        panel.Controls.Add(label);
        return panel;
    }

    private void AddRow(string labelText, Control editor)
    {
        int row = _layout.RowCount++;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = labelText,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(3, 6, 3, 6)
        };

        editor.Margin = new Padding(3, 3, 3, 3);
        editor.Dock = DockStyle.Fill;

        _layout.Controls.Add(label, 0, row);
        _layout.Controls.Add(editor, 1, row);
    }

    private void AddFullWidthRow(Control control)
    {
        int row = _layout.RowCount++;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        control.Dock = DockStyle.Fill;
        _layout.Controls.Add(control, 0, row);
        _layout.SetColumnSpan(control, 2);
    }
    private void UpdateSettings()
    {
        Settings = new MouseSettings
        {
            Color = _selectedColor,
            Size = (float)_size.Value,
            LifeTime = (float)_lifeTime.Value,
            SpawnRate = (float)_spawnRate.Value,
            SpawnRatePerUnit = (float)_spawnRatePerUnit.Value,
            Velocity = (float)_velocity.Value
        };

        SettingsApplied?.Invoke(Settings);
    }
    private static Color VectorToColor(Vector3 v)
    {
        int r = (int)(Math.Clamp(v.X, 0f, 1f) * 255f);
        int g = (int)(Math.Clamp(v.Y, 0f, 1f) * 255f);
        int b = (int)(Math.Clamp(v.Z, 0f, 1f) * 255f);

        return Color.FromArgb(r, g, b);
    }

    private static Vector3 ColorToVector(Color c)
    {
        return new Vector3(
            c.R / 255f,
            c.G / 255f,
            c.B / 255f);
    }
    private void OnCloseClick(object? sender, EventArgs e)
    {
        ShouldBeClosed = true;
        Close();
    }
}