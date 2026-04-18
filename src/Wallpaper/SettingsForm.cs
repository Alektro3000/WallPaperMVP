using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Windows.Forms;

public sealed class SettingsForm : Form
{
    private readonly SettingsStore _store;
    
    public event EventHandler? ExitRequested;
    public SettingsForm(SettingsStore store)
    {
        _store = store;

        Text = "Particle Settings";
        Width = 420;
        Height = 700;

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        BuildTabsFromSystemSettings(tabs);

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
            ExitRequested?.Invoke(this, EventArgs.Empty);
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
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void BuildTabsFromSystemSettings(TabControl tabs)
    {
        var sectionFields = typeof(SystemSettings).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var sectionField in sectionFields)
        {
            var sectionValue = sectionField.GetValue(_store.GetSnapshot());
            if (sectionValue == null)
                continue;

            var tab = new TabPage(ToDisplayName(sectionField.Name));
            var panel = CreatePanel(tab);

            BuildControlsForSection(panel, sectionField);

            tabs.TabPages.Add(tab);
        }
    }

    private void BuildControlsForSection(FlowLayoutPanel panel, FieldInfo sectionField)
    {
        var sectionType = sectionField.FieldType;
        var childFields = sectionType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        var settings = _store.GetSnapshot();

        foreach (var childField in childFields)
        {
            var label = childField.GetCustomAttribute<UiLabelAttribute>()?.Label ?? ToDisplayName(childField.Name);
            var fieldType = childField.FieldType;

            if (fieldType == typeof(float))
            {
                AddFloatField(settings, panel, sectionField, childField, label);
            }
            else if (fieldType == typeof(Vector2))
            {
                AddVector2Field(settings, panel, sectionField, childField, label);
            }
            else if (fieldType == typeof(Vector3) && childField.GetCustomAttribute<UiColorAttribute>() != null)
            {
                AddColor3Field(settings, panel, sectionField, childField, label);
            }
            else if (fieldType == typeof(Vector4) && childField.GetCustomAttribute<UiColorAttribute>() != null)
            {
                AddColor4Field(settings, panel, sectionField, childField, label);
            }
        }
    }

    private void AddFloatField(SystemSettings settings, FlowLayoutPanel panel, FieldInfo sectionField, FieldInfo childField, string label)
    {
        var sectionValue = sectionField.GetValue(settings)!;
        float value = (float)childField.GetValue(sectionValue)!;

        var range = childField.GetCustomAttribute<UiRangeAttribute>();
        decimal min = range != null ? (decimal)range.Min : -100000m;
        decimal max = range != null ? (decimal)range.Max : 100000m;
        decimal step = range != null ? (decimal)range.Step : 0.01m;

        AddFloat(panel, label, value, min, max, step, (v, root) =>
        {
            var section = sectionField.GetValue(root)!;
            childField.SetValue(section, v);
            sectionField.SetValue(root, section);
        });
    }

    private void AddVector2Field(SystemSettings settings, FlowLayoutPanel panel, FieldInfo sectionField, FieldInfo childField, string label)
    {
        var sectionValue = sectionField.GetValue(settings)!;
        Vector2 value = (Vector2)childField.GetValue(sectionValue)!;

        var meta = childField.GetCustomAttribute<UiVector2Attribute>();

        string xLabel = meta?.XLabel ?? "X";
        string yLabel = meta?.YLabel ?? "Y";

        decimal minX = meta != null ? (decimal)meta.MinX : -100000m;
        decimal maxX = meta != null ? (decimal)meta.MaxX : 100000m;
        decimal stepX = meta != null ? (decimal)meta.StepX : 0.01m;

        decimal minY = meta != null ? (decimal)meta.MinY : -100000m;
        decimal maxY = meta != null ? (decimal)meta.MaxY : 100000m;
        decimal stepY = meta != null ? (decimal)meta.StepY : 0.01m;

        AddFloat(panel, $"{label} {xLabel}", value.X, minX, maxX, stepX, (v, root) =>
        {
            var section = sectionField.GetValue(root)!;

            var cur = (Vector2)childField.GetValue(section)!;
            cur.X = v;
            childField.SetValue(section, cur);

            sectionField.SetValue(root, section);
        });

        AddFloat(panel, $"{label} {yLabel}", value.Y, minY, maxY, stepY, (v, root) =>
        {
            var section = sectionField.GetValue(root)!;
            var cur = (Vector2)childField.GetValue(section)!;
            cur.Y = v;
            childField.SetValue(section, cur);
            sectionField.SetValue(root, section);
        });
    }
    private void AddColor3Field(SystemSettings settings, FlowLayoutPanel panel, FieldInfo sectionField, FieldInfo childField, string label)
    {
        var sectionValue = sectionField.GetValue(settings)!;
        Vector3 value = (Vector3)childField.GetValue(sectionValue)!;

        var meta = childField.GetCustomAttribute<UiColorAttribute>();
        bool normalized = meta?.Normalized ?? true;

        var row = new Panel
        {
            Width = 340,
            Height = 36
        };

        var text = new Label
        {
            Text = label,
            Left = 0,
            Top = 10,
            Width = 150
        };

        var preview = new Panel
        {
            Left = 160,
            Top = 6,
            Width = 40,
            Height = 24,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ToColor(value, normalized)
        };

        var button = new Button
        {
            Text = "Pick...",
            Left = 210,
            Top = 4,
            Width = 80,
            Height = 28
        };

        button.Click += (_, _) =>
        {
            var section = sectionField.GetValue(_store.GetSnapshot())!;
            var current = (Vector3)childField.GetValue(section)!;

            using var dlg = new ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true,
                SolidColorOnly = false,
                Color = ToColor(current, normalized)
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _store.Update( set =>
                {
                    var section = sectionField.GetValue(set)!;

                    var newValue = FromColor(dlg.Color, normalized);
                    childField.SetValue(section, newValue);
                    sectionField.SetValue(set, section);
                });

                preview.BackColor = dlg.Color;
            }
        };

        row.Controls.Add(text);
        row.Controls.Add(preview);
        row.Controls.Add(button);

        panel.Controls.Add(row);
    }

    private void AddColor4Field(SystemSettings settings, FlowLayoutPanel panel, FieldInfo sectionField, FieldInfo childField, string label)
    {
        var sectionValue = sectionField.GetValue(settings)!;
        Vector4 value = (Vector4)childField.GetValue(sectionValue)!;

        var meta = childField.GetCustomAttribute<UiColorAttribute>();
        bool normalized = meta?.Normalized ?? true;

        decimal min = 0m;
        decimal max = normalized ? 1m : 255m;
        decimal step = normalized ? 0.01m : 1m;

        AddFloat(panel, $"{label} R", value.X, min, max, step, (v, root) =>
        {
            var section = sectionField.GetValue(root)!;
            var cur = (Vector4)childField.GetValue(section)!;
            cur.X = v;
            childField.SetValue(section, cur);
            sectionField.SetValue(root, section);
        });

        AddFloat(panel, $"{label} G", value.Y, min, max, step, (v, root) =>
        {
            var section = sectionField.GetValue(root)!;
            var cur = (Vector4)childField.GetValue(section)!;
            cur.Y = v;
            childField.SetValue(section, cur);
            sectionField.SetValue(root, section);
        });

        AddFloat(panel, $"{label} B", value.Z, min, max, step, (v, root) =>
        {
            var section = sectionField.GetValue(root)!;
            var cur = (Vector4)childField.GetValue(section)!;
            cur.Z = v;
            childField.SetValue(section, cur);
            sectionField.SetValue(root, section);
        });

        AddFloat(panel, $"{label} A", value.W, min, max, step, (v, root) =>
        {
            var section = sectionField.GetValue(root)!;
            var cur = (Vector4)childField.GetValue(section)!;
            cur.W = v;
            childField.SetValue(section, cur);
            sectionField.SetValue(root, section);
        });
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

    private void AddFloat(
        FlowLayoutPanel panel,
        string label,
        float value,
        decimal min,
        decimal max,
        decimal increment,
        Action<float, SystemSettings> setter)
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
            Width = 140,
            DecimalPlaces = GetDecimalPlaces(increment),
            Increment = increment,
            Minimum = min,
            Maximum = max,
            Value = ClampToRange((decimal)value, min, max)
        };

        box.ValueChanged += (_, _) =>
        {
            _store.Update(root =>
            {
                setter((float)box.Value, root);
            });
        };

        row.Controls.Add(text);
        row.Controls.Add(box);
        panel.Controls.Add(row);
    }

    private static decimal ClampToRange(decimal value, decimal min, decimal max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static string ToDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        if (name.EndsWith("Settings", StringComparison.Ordinal))
            name = name[..^"Settings".Length];

        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    private static Color ToColor(Vector3 value, bool normalized)
    {
        int r, g, b;

        if (normalized)
        {
            r = FloatToByte(value.X);
            g = FloatToByte(value.Y);
            b = FloatToByte(value.Z);
        }
        else
        {
            r = ClampByte((int)MathF.Round(value.X));
            g = ClampByte((int)MathF.Round(value.Y));
            b = ClampByte((int)MathF.Round(value.Z));
        }

        return Color.FromArgb(r, g, b);
    }

    private static Vector3 FromColor(Color color, bool normalized)
    {
        if (normalized)
        {
            return new Vector3(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f);
        }

        return new Vector3(color.R, color.G, color.B);
    }

    private static int FloatToByte(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        return (int)MathF.Round(x * 255f);
    }

    private static int ClampByte(int x)
    {
        if (x < 0) return 0;
        if (x > 255) return 255;
        return x;
    }
    private static int GetDecimalPlaces(decimal increment)
    {
        increment = Math.Abs(increment);

        int places = 0;

        while (increment < 1m && increment != Math.Truncate(increment))
        {
            increment *= 10m;
            places++;

            // avoid accidental infinite loop
            if (places > 10)
                break;
        }

        return places;
    }

}