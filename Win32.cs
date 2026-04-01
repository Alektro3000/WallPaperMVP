using System;
using System.Runtime.InteropServices;

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

    public const int WM_DESTROY = 0x0002;
    public const int WM_QUIT = 0x0012;
    public const int WM_HOTKEY = 0x0312;
    public const int PM_REMOVE = 0x0001;

    public const int HOTKEY_ID = 1;

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    public const int SW_SHOW = 5;

    private const int WM_SPAWN_WORKER = 0x052C;


    // Ctrl + Shift + F10
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT   = 0x0004;
    public const uint VK_F10      = 0x79;


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
        string className = "WallpaperClass";

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
            lpszClassName = className
        };

        RegisterClassEx(ref wc);

        IntPtr hwnd = CreateWindowEx(
            WS_EX_LAYERED | WS_EX_TRANSPARENT, // Correct combination for click-through
            className,
            "Wallpaper",
            WS_POPUP | WS_VISIBLE,
            0, 0, width, height,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        return hwnd;
    }

    // =========================
    // Attach to wallpaper layer
    // =========================
    public static void AttachToWallpaper(IntPtr hwnd)
    {
        IntPtr progman = FindWindow("Progman", null);

        SendMessageTimeout(
            progman,
            WM_SPAWN_WORKER,
            IntPtr.Zero,
            IntPtr.Zero,
            0,
            1000,
            out _);

        IntPtr workerW = IntPtr.Zero;

        while (true)
        {
            IntPtr worker = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
            if (worker == IntPtr.Zero)
                break;

            IntPtr shellView = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (shellView != IntPtr.Zero)
            {
                workerW = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
                break;
            }

            workerW = worker;
        }

        SetParent(hwnd, workerW);
        ShowWindow(hwnd, SW_SHOW);
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
    
}
