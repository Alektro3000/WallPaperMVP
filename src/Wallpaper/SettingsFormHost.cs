using System;
using System.Threading;
using System.Windows.Forms;
using Particles.Systems;
using Particles.Settings;

public sealed class SettingsFormHost : IDisposable
{
    private readonly SettingsStore _store;

    private Thread? _thread;
    private SettingsForm? _form;
    private NotifyIcon? _trayIcon;
    private Control? _uiInvoker;

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

        _uiInvoker = new Control();
        _uiInvoker.CreateControl();

        _trayIcon = CreateTrayIcon();

        _formReady.Set();
        Application.Run();

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayIcon = null;

        _uiInvoker.Dispose();
        _uiInvoker = null;
    }

    public void ShowForm()
    {
        var invoker = _uiInvoker;
        if (invoker == null || invoker.IsDisposed)
            return;

        if (invoker.InvokeRequired)
        {
            invoker.BeginInvoke(new Action(ShowFormOnUiThread));
        }
        else
        {
            ShowFormOnUiThread();
        }
    }

    private void ShowFormOnUiThread()
    {
        if (_form != null && !_form.IsDisposed)
        {
            ShowAndActivate(_form);
            return;
        }

        var form = new SettingsForm(_store);
        _form = form;

        form.ExitRequested += (_, _) =>
        {
            Interlocked.Exchange(ref _exitRequested, 1);

            if (_trayIcon != null)
                _trayIcon.Visible = false;

            Application.ExitThread();
        };

        form.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_form, form))
                _form = null;
        };

        ShowAndActivate(form);
    }

    public void RequestCloseForm()
    {
        var invoker = _uiInvoker;
        if (invoker == null || invoker.IsDisposed)
            return;

        if (invoker.InvokeRequired)
        {
            invoker.BeginInvoke(new Action(RequestCloseFormOnUiThread));
        }
        else
        {
            RequestCloseFormOnUiThread();
        }
    }

    private void RequestCloseFormOnUiThread()
    {
        if (_form == null || _form.IsDisposed)
            return;

        _form.Close();
    }

    private static void ShowAndActivate(Form form)
    {
        if (!form.Visible)
            form.Show();

        if (form.WindowState == FormWindowState.Minimized)
            form.WindowState = FormWindowState.Normal;

        form.ShowInTaskbar = true;
        form.BringToFront();
        form.Activate();
    }

    private NotifyIcon CreateTrayIcon()
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
            Application.ExitThread();
        });

        trayIcon.ContextMenuStrip = menu;
        trayIcon.DoubleClick += (_, _) => ShowForm();

        return trayIcon;
    }

    public void Dispose()
    {
        RequestCloseForm();

        _uiInvoker?.BeginInvoke(new Action(Application.ExitThread));
        
        _formReady.Dispose();
    }
}