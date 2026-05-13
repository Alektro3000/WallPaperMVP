using System.Numerics;
using System.Reflection;
using Settings;

public sealed class SettingsForm : Form
{
    private sealed record FieldPath(Type InitType, FieldInfo[] Fields)
    {
        public object? GetValue(SystemSettings root)
        {
            object? current = root.GetSettings(InitType);

            foreach (var field in Fields)
                current = field.GetValue(current);

            return current;
        }

        public void SetValue(SystemSettings root, object? value)
        {
            SetValueRecursive(root.GetSettings(InitType), 0, value);
        }

        private void SetValueRecursive(object current, int index, object? value)
        {
            var field = Fields[index];

            if (index == Fields.Length - 1)
            {
                field.SetValue(current, value);
                return;
            }

            var child = field.GetValue(current)!;

            SetValueRecursive(child, index + 1, value);

            // Important for structs: write modified child back into parent
            field.SetValue(current, child);
        }
    }

    private readonly SettingsStore _store;

    public event EventHandler? ExitRequested;
    protected override void OnResizeBegin(EventArgs e)
    {
        base.OnResizeBegin(e);
        SuspendLayout();
    }

    protected override void OnResizeEnd(EventArgs e)
    {
        ResumeLayout(true);
        base.OnResizeEnd(e);
    }
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

        Controls.Add(buttons);
    }


    private void BuildTabsFromSystemSettings(TabControl tabs)
    {
        var systemValues = _store.GetSnapshot();
        foreach (var settingsTypes in systemValues.GetSavedTypes())
        {
            var tab = new TabPage(ToDisplayName(settingsTypes.FullName ?? settingsTypes.Name));
            var panel = CreatePanel(tab);

            BuildControlsForSection(panel, systemValues, settingsTypes);

            tabs.TabPages.Add(tab);
        }
    }

    private void BuildControlsForSection(FlowLayoutPanel panel, SystemSettings settings, Type FieldType)
    {
        AddFieldsRecursive(settings, panel, FieldType,
            FieldType,

            []);
    }

    private void AddFieldsRecursive(
        SystemSettings settings,
        FlowLayoutPanel panel, Type initType,
        Type type,
        List<FieldInfo> path)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var fieldType = field.FieldType;
            var label = field.GetCustomAttribute<UiLabelAttribute>()?.Label
                        ?? ToDisplayName(field.Name);

            var currentPath = path.Append(field).ToArray();
            var fieldPath = new FieldPath(initType, currentPath);

            if (fieldType == typeof(float))
            {
                AddFloatField(settings, panel, fieldPath, field, label);
            }
            else if (fieldType == typeof(Vector2))
            {
                AddVector2Field(settings, panel, fieldPath, field, label);
            }
            else if (fieldType == typeof(Vector3) &&
                    field.GetCustomAttribute<UiColorAttribute>() != null)
            {
                AddColor3Field(settings, panel, fieldPath, field, label);
            }
            else if (fieldType == typeof(Vector4) &&
                    field.GetCustomAttribute<UiColorAttribute>() != null)
            {
                AddColor4Field(settings, panel, fieldPath, field, label);
            }
            else if (fieldType == typeof(string))
            {
                AddStringField(settings, panel, fieldPath, field, label);
            }
            else if (!fieldType.IsPrimitive &&
                    !fieldType.IsEnum &&
                    fieldType != typeof(string))
            {
                AddFieldsRecursive(settings, panel, initType, fieldType, currentPath.ToList());
            }
        }
    }
    private void AddStringField(
        SystemSettings settings,
        FlowLayoutPanel panel,
        FieldPath path,
        FieldInfo childField,
        string label)
    {
        string value = (string?)path.GetValue(settings) ?? "";

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

        var box = new TextBox
        {
            Left = 160,
            Top = 4,
            Width = 160,
            Text = value
        };

        box.TextChanged += (_, _) =>
        {
            _store.Update(root =>
            {
                path.SetValue(root, box.Text);
            });

            _store.Save();
        };

        row.Controls.Add(text);
        row.Controls.Add(box);
        panel.Controls.Add(row);
    }
    private void AddFloatField(SystemSettings settings, FlowLayoutPanel panel, FieldPath path, FieldInfo childField, string label)
    {
        float value = (float)path.GetValue(settings)!;

        var range = childField.GetCustomAttribute<UiRangeAttribute>();
        decimal min = range != null ? (decimal)range.Min : -100000m;
        decimal max = range != null ? (decimal)range.Max : 100000m;
        decimal step = range != null ? (decimal)range.Step : 0.01m;

        AddFloat(panel, label, value, min, max, step, (v, root) =>
        {
            path.SetValue(root, v);
        });
    }

    private void AddVector2Field(SystemSettings settings, FlowLayoutPanel panel, FieldPath path, FieldInfo childField, string label)
    {
        Vector2 value = (Vector2)path.GetValue(settings)!;

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
            var cur = (Vector2)path.GetValue(root)!;
            cur.X = v;
            path.SetValue(root, cur);
        });

        AddFloat(panel, $"{label} {yLabel}", value.Y, minY, maxY, stepY, (v, root) =>
        {
            var cur = (Vector2)path.GetValue(root)!;
            cur.Y = v;
            path.SetValue(root, cur);
        });
    }
    private void AddColor3Field(SystemSettings settings, FlowLayoutPanel panel, FieldPath path, FieldInfo childField, string label)
    {
        Vector3 value = (Vector3)path.GetValue(settings)!;

        var meta = childField.GetCustomAttribute<UiColorAttribute>();

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
            BackColor = ToColor(value)
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
            var current = (Vector3)path.GetValue(_store.GetSnapshot())!;

            using var dlg = new ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true,
                SolidColorOnly = false,
                Color = ToColor(current)
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _store.Update(set =>
                {
                    var newValue = FromColor(dlg.Color);
                    path.SetValue(set, newValue);
                });

                preview.BackColor = dlg.Color;

                _store.Save();
            }
        };

        row.Controls.Add(text);
        row.Controls.Add(preview);
        row.Controls.Add(button);

        panel.Controls.Add(row);
    }
    private void AddColor4Field(
        SystemSettings settings,
        FlowLayoutPanel panel,
        FieldPath path,
        FieldInfo field,
        string label)
    {
        Vector4 value = (Vector4)path.GetValue(settings)!;

        var meta = field.GetCustomAttribute<UiColorAttribute>();
        bool normalized = meta?.Normalized ?? true;

        decimal min = 0m;
        decimal max = normalized ? 1m : 255m;
        decimal step = normalized ? 0.01m : 1m;

        AddFloat(panel, $"{label} R", value.X, min, max, step, (v, root) =>
        {
            var cur = (Vector4)path.GetValue(root)!;
            cur.X = v;
            path.SetValue(root, cur);
        });

        AddFloat(panel, $"{label} G", value.Y, min, max, step, (v, root) =>
        {
            var cur = (Vector4)path.GetValue(root)!;
            cur.Y = v;
            path.SetValue(root, cur);
        });

        AddFloat(panel, $"{label} B", value.Z, min, max, step, (v, root) =>
        {
            var cur = (Vector4)path.GetValue(root)!;
            cur.Z = v;
            path.SetValue(root, cur);
        });

        AddFloat(panel, $"{label} A", value.W, min, max, step, (v, root) =>
        {
            var cur = (Vector4)path.GetValue(root)!;
            cur.W = v;
            path.SetValue(root, cur);
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
            _store.Save();
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

        if (name.EndsWith(".Settings", StringComparison.Ordinal))
            name = name[..^".Settings".Length];

        if (name.StartsWith("Particles.Systems.", StringComparison.Ordinal))
            name = name["Particles.Systems.".Length..];

        if (name.StartsWith("Particles.Shared.", StringComparison.Ordinal))
            name = name["Particles.Shared.".Length..];

        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    private static Color ToColor(Vector3 value)
    {
        int r, g, b;

        r = FloatToByte(value.X);
        g = FloatToByte(value.Y);
        b = FloatToByte(value.Z);

        return Color.FromArgb(r, g, b);
    }

    private static Vector3 FromColor(Color color)
    {
        return new Vector3(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f);
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