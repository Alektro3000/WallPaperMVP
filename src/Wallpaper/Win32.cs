using System;
using System.Runtime.InteropServices;
using Serilog;

public static class Win32
{
    // =========================
    // Constants
    // =========================

    // Define the DPI awareness context constant
    public const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    public const int WS_POPUP = unchecked((int)0x80000000);
    public const int WS_VISIBLE = 0x10000000;

    public const int WS_EX_LAYERED = 0x80000;
    public const int WS_EX_TRANSPARENT = 0x20;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public const int WM_DESTROY = 0x0002;
    public const int WM_QUIT = 0x0012;
    public const int WM_HOTKEY = 0x0312;
    public const int PM_REMOVE = 0x0001;

    public const int HOTKEY_ID = 1;

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    public const int SW_SHOW = 5;

    private const int WM_SPAWN_WORKER = 0x052C;

    private const int LWA_COLORKEY = 0x00000001;
    private const int LWA_ALPHA = 0x00000002;


    // Ctrl + Shift + F10
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint VK_F10 = 0x79;


    // =========================
    // Structs
    // =========================
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    // =========================
    // Delegate (IMPORTANT: keep reference!)
    // =========================
    public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // =========================
    // Window creation
    // =========================
    public static IntPtr CreateWallpaperWindow(int width, int height, WndProc wndProcDelegate)
    {
        Log.Debug("Creating Main Window;");
        string className = "WallpaperClass";

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
            lpszClassName = className
        };

        RegisterClassEx(ref wc);

        IntPtr hwnd = CreateWindowEx(
            WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE, // Correct combination for click-through
            className,
            "Wallpaper",
            WS_POPUP | WS_VISIBLE,
            0, 0, width, height,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
        SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);

        Log.Debug("Window created with {Hwnd} hwnd", hwnd);

        SetWindowPos(
            hwnd,
            HWND_BOTTOM,
            0, 0, width, height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);



        Log.Debug("Window {Hwnd} hwnd moved to background", hwnd);

        return hwnd;
    }

    // =========================
    // Attach to wallpaper layer
    // =========================
    public static void AttachToWallpaper(IntPtr hwnd)
    {
        Log.Information("Attaching window {Hwnd} to wallpaper layer", hwnd);

        IntPtr progman = FindWindow("Progman", null);

        if (progman == IntPtr.Zero)
        {
            Log.Error("Failed to find Progman window");
            throw new InvalidOperationException("Could not find Progman window");
        }
        Log.Debug("Found Progman window: {Progman}", progman);

        IntPtr progmanShellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        IntPtr targetWorkerW;
        if (progmanShellView != IntPtr.Zero)
        {
            Log.Warning("Progman has SHELLDLL_DefView {ShellView}", progmanShellView);
        }
        targetWorkerW = AttachSelector(progman);

        if (targetWorkerW == IntPtr.Zero)
        {
            Log.Error("Failed to find target window for wallpaper attachment");
            throw new InvalidOperationException("Could not locate WorkerW wallpaper host");
        }

        IntPtr previousParent = SetParent(hwnd, targetWorkerW);


        Log.Information(
            "Attached window {Hwnd} to target {target}; previous parent was {PreviousParent}",
            hwnd,
            targetWorkerW,
            previousParent);

        ShowWindow(hwnd, SW_SHOW);
    }
    public static IntPtr AttachToWallpaperAlt(IntPtr hwnd, IntPtr progman)
    {
        SendMessageTimeout(
            progman,
            WM_SPAWN_WORKER,
            IntPtr.Zero,
            IntPtr.Zero,
            0,
            1000,
            out _);

        IntPtr shellViewInProgman = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        Log.Debug("Progman direct SHELLDLL_DefView = {ShellView}", shellViewInProgman);

        IntPtr searchAfter = IntPtr.Zero;
        IntPtr best = IntPtr.Zero;

        while (true)
        {
            IntPtr worker = FindWindowEx(IntPtr.Zero, searchAfter, "WorkerW", null);
            if (worker == IntPtr.Zero)
                break;

            searchAfter = worker;

            Log.Debug("Examining WorkerW {Worker}", worker);


            // skip if it hosts desktop icons
            IntPtr shellView = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                Log.Debug("Skipping WorkerW {Worker}: hosts SHELLDLL_DefView {ShellView}", worker, shellView);
                continue;
            }

            // fallback: remember last reasonable candidate
            Log.Debug("Remembering WorkerW {Worker} as candidate", worker);
            best = worker;
        }

        if (best == IntPtr.Zero)
        {
            Log.Error("No suitable WorkerW found in Alt");
        }
        return best;
    }

    public static IntPtr AttachSelector(IntPtr progman)
    {
        Log.Debug("Sending WM_SPAWN_WORKER message to Progman");

        SendMessageTimeout(
            progman,
            WM_SPAWN_WORKER,
            IntPtr.Zero,
            IntPtr.Zero,
            0,
            1000,
            out _);

        IntPtr searchAfter = IntPtr.Zero;
        IntPtr targetWorkerW = IntPtr.Zero;

        while (true)
        {
            IntPtr worker = FindWindowEx(IntPtr.Zero, searchAfter, "WorkerW", null);
            if (worker == IntPtr.Zero)
                break;

            Log.Debug("Enumerated WorkerW {Worker}", worker);

            IntPtr shellView = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (shellView != IntPtr.Zero)
            {
                Log.Debug(
                    "Found SHELLDLL_DefView {ShellView} inside WorkerW {Worker}",
                    shellView,
                    worker);

                targetWorkerW = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
                Log.Debug("Next WorkerW after shell host is {TargetWorkerW}", targetWorkerW);
                break;
            }

            searchAfter = worker;
        }

        if (targetWorkerW == IntPtr.Zero)
        {
            Log.Error("No suitable WorkerW found in Base");
        }
        return targetWorkerW;
    }

    // =========================
    // Win32 imports
    // =========================
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr CreateWindowEx(
        int dwExStyle,
        string lpClassName,
        string lpWindowName,
        int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint min, uint max, uint remove);

    [DllImport("user32.dll")]
    public static extern bool TranslateMessage(ref MSG msg);

    [DllImport("user32.dll")]
    public static extern IntPtr DispatchMessage(ref MSG msg);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(
        IntPtr hwnd,
        uint crKey,
        byte bAlpha,
        uint dwFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string? windowTitle);

    [DllImport("user32.dll")]
    public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int Msg,
        IntPtr wParam,
        IntPtr lParam,
        int flags,
        int timeout,
        out IntPtr result);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(int value);


    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);


    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
    IntPtr hWnd,
    IntPtr hWndInsertAfter,
    int X,
    int Y,
    int cx,
    int cy,
    uint uFlags);
}
