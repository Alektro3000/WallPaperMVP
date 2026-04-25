using System;
using System.Threading;
using System.Windows.Forms;
using ParticleSystems;

public sealed class SettingsFormHost : IDisposable
{
    private readonly SettingsStore _store;

    private Thread? _thread;
    private SettingsForm? _form;
    private NotifyIcon? _trayIcon;

    private readonly ManualResetEventSlim _formReady = new(false);
    private int _exitRequested;

    public bool ExitRequested => Volatile.Read(ref _exitRequested) != 0;

    public SettingsFormHost(SettingsStore store)
    {
        _store = store;
    }

    public void Start()
    {
        _thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Name = "Settings UI",
            Priority = ThreadPriority.BelowNormal
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        _formReady.Wait();
    }

    private void ThreadMain()
    {
        ApplicationConfiguration.Initialize();

        var form = new SettingsForm(_store);
        _form = form;

        form.ExitRequested += (_, _) =>
        {
            Interlocked.Exchange(ref _exitRequested, 1);
        };
        form.Load += (_, _) => form.Hide();

        _trayIcon = CreateTrayIcon(form);

        // start hidden
        form.ShowInTaskbar = false;
        form.WindowState = FormWindowState.Minimized;
        form.Hide();

        _formReady.Set();
        Application.Run(form);

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayIcon = null;

        form.Dispose();
        _form = null;
    }

    public void ShowForm()
    {
        var form = _form;
        if (form == null || form.IsDisposed)
            return;

        if (form.InvokeRequired)
        {
            form.BeginInvoke(new Action(() =>
            {
                ShowAndActivate(form);
            }));
        }
        else
        {
            ShowAndActivate(form);
        }
    }

    public void RequestCloseForm()
    {
        var form = _form;
        if (form == null || form.IsDisposed)
            return;

        if (form.InvokeRequired)
        {
            form.BeginInvoke(new Action(() => form.Close()));
        }
        else
        {
            form.Close();
        }
    }

    private static void ShowAndActivate(Form form)
    {
        if (!form.Visible)
            form.Show();

        if (form.WindowState == FormWindowState.Minimized)
            form.WindowState = FormWindowState.Normal;

        form.BringToFront();
        form.Activate();
    }

    private NotifyIcon CreateTrayIcon(SettingsForm form)
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");

        NotifyIcon trayIcon = new NotifyIcon
        {
            Icon = new Icon(iconPath),
            Text = "Wallpaper",
            Visible = true
        };

        ContextMenuStrip menu = new ContextMenuStrip();

        menu.Items.Add("Settings", null, (_, _) =>
        {
            ShowForm();
        });

        menu.Items.Add("Exit", null, (_, _) =>
        {
            Interlocked.Exchange(ref _exitRequested, 1);
            trayIcon.Visible = false;
        });

        trayIcon.ContextMenuStrip = menu;
        trayIcon.DoubleClick += (_, _) => ShowForm();

        return trayIcon;
    }

    public void Dispose()
    {
        RequestCloseForm();
        _formReady.Dispose();
    }
}