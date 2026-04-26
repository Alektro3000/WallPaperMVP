using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Particles.Settings;
using Particles.Systems;
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
            .MinimumLevel.Information()
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
        catch (InvalidOperationException)
        {
        }
        Win32.RegisterHotKey(
            hwnd,
            Win32.HOTKEY_ID,
            Win32.MOD_CONTROL | Win32.MOD_SHIFT,
            Win32.VK_F10);

        try
        {
            var settingsPath = Path.Combine(
                AppContext.BaseDirectory,
                "settings.json");
            var store = new SettingsStore(settingsPath, new SystemSettings());

            using var formHost = new SettingsFormHost(store);
            formHost.Start();
            Log.Information("Form Settings initialized");

            Log.Debug("Renderer begin initialization");
            using var renderer = new Renderer.Orchestrator(store.GetSnapshot(), hwnd, width, height);
            Log.Information("Renderer initialized");




            Win32.MSG msg;
            while (true)
            {
                while (Win32.PeekMessage(out msg, IntPtr.Zero, 0, 0, Win32.PM_REMOVE))
                {
                    if (msg.message == Win32.WM_QUIT)
                        return;

                    if (msg.message == Win32.WM_HOTKEY && msg.wParam.ToInt32() == Win32.HOTKEY_ID)
                    {
                        formHost.ShowForm();
                    }

                    Win32.TranslateMessage(ref msg);
                    Win32.DispatchMessage(ref msg);
                }
                if (formHost.ExitRequested)
                {
                    Log.Information("Close via form");
                    return;
                }
                renderer.Render(store.GetSnapshot());

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

}