using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Serilog;

class Program
{

    // =========================
    // Window procedure
    // =========================
    public static IntPtr WndProcImpl(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == Win32.WM_DESTROY)
        {
            Win32.PostQuitMessage(0);
        }

        return Win32.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    static void Main()
    {
        Win32.SetProcessDpiAwarenessContext(Win32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();
        
        Log.Information("App started");


        int width = Win32.GetSystemMetrics(Win32.SM_CXSCREEN);
        int height = Win32.GetSystemMetrics(Win32.SM_CYSCREEN);

        Log.Information("Window size is {Width}x{Height}", width, height);
        IntPtr hwnd = Win32.CreateWallpaperWindow(width, height, WndProcImpl);
        try
        {
            Win32.AttachToWallpaper(hwnd);
        }
        catch (InvalidOperationException _)
        {
        }
        Win32.RegisterHotKey(
            hwnd,
            Win32.HOTKEY_ID,
            Win32.MOD_CONTROL | Win32.MOD_SHIFT,
            Win32.VK_F10);

        try
        {

            var settings = new SystemSettings();
            using var form = new SettingsForm(settings);
            Log.Information("Form Settings initialized");
            using var renderer = new Renderer(hwnd, width, height, settings);
            Log.Information("Renderer initialized");

            using var icon = CreateTrayIcon(form);
            Log.Information("Tray Icon Created");



            Win32.MSG msg;
            while (true)
            {
                while (Win32.PeekMessage(out msg, IntPtr.Zero, 0, 0, Win32.PM_REMOVE))
                {
                    if (msg.message == Win32.WM_QUIT)
                        return;

                    if (msg.message == Win32.WM_HOTKEY && msg.wParam.ToInt32() == Win32.HOTKEY_ID)
                    {
                        form.Show();
                    }

                    Win32.TranslateMessage(ref msg);
                    Win32.DispatchMessage(ref msg);
                }
                if (form.ShouldBeClosed)
                {
                    Log.Information("Close via form");
                    return;
                }
                renderer.Render();

            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed unexpectedly");
            throw;
        }
        finally
        {
            Win32.UnregisterHotKey(hwnd, Win32.HOTKEY_ID);
            Log.CloseAndFlush();
        }
    }

    static NotifyIcon CreateTrayIcon(SettingsForm form)
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
        Log.Debug("Creating tray icon from {IconPath}", iconPath);
        NotifyIcon trayIcon = new NotifyIcon
        {
            Icon = new Icon(iconPath),
            Text = "Wallpaper",
            Visible = true
        };

        // Optional context menu
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (s, e) =>
        {
            form.Show();
        });

        menu.Items.Add("Exit", null, (s, e) =>
        {
            form.ShouldBeClosed = true;
            trayIcon.Visible = false;
        });

        trayIcon.ContextMenuStrip = menu;
        return trayIcon;
    }
}