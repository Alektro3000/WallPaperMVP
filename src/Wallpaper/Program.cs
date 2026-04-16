using System;
using System.Runtime.InteropServices;

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


        int width = Win32.GetSystemMetrics(Win32.SM_CXSCREEN);
        int height = Win32.GetSystemMetrics(Win32.SM_CYSCREEN);

        IntPtr hwnd = Win32.CreateWallpaperWindow(width, height, WndProcImpl);
        Win32.AttachToWallpaper(hwnd);

        Win32.RegisterHotKey(
            hwnd,
            Win32.HOTKEY_ID,
            Win32.MOD_CONTROL | Win32.MOD_SHIFT,
            Win32.VK_F10);

        try
        {
            using var renderer = new Renderer(hwnd, width, height);
            var form = new SettingsForm(new MouseSettings());
            form.SettingsApplied += (setting) => {
                (renderer.ParticleSystems.First(x=>x is MouseSystem) as MouseSystem)
                    ?.UpdateMouseSettings(setting);
            };
            Win32.MSG msg;
            
        NotifyIcon trayIcon = new NotifyIcon
        {
            Icon = new Icon("app.ico"),
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

        // Double-click handler
        trayIcon.DoubleClick += (s, e) =>
        {
            MessageBox.Show("Tray icon double-clicked");
        };


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
                if(form.ShouldBeClosed)
                    return;
                renderer.Render();
                
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        finally
        {
            Win32.UnregisterHotKey(hwnd, Win32.HOTKEY_ID);
        }
    }
}