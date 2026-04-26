using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Particles.Shared.Field;

public static class WindowEnumerator
{

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    public record WindowInfo(IntPtr Handle, String Name,RECT Rect);


    [DllImport("user32.dll")]
    static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);


    const int GWL_EXSTYLE = -20;
    const int WS_EX_TOOLWINDOW = 0x00000080;

    public static List<WindowInfo> GetWindows()
    {
        var result = new List<WindowInfo>();
        IntPtr shellWindow = GetShellWindow();

        EnumWindows((hWnd, _) =>
        {
            if (hWnd == shellWindow)
                return true;

            if (!IsWindowVisible(hWnd) || IsIconic(hWnd))
                return true;

            // Skip tool windows
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                return true;

            // Skip windows with no title
            int titleLength = GetWindowTextLength(hWnd);
            if (titleLength == 0)
                return true;

            if (!GetWindowRect(hWnd, out var rect))
                return true;

            // Skip zero-size / untitled helper windows if desired
            if (rect.Width <= 0 || rect.Height <= 0)
                return true;

            //Don't ask for now
            if (rect.Width >= 1920 || rect.Height >= 1080)
                return true;
                
            string className = GetWindowClass(hWnd);
            if (className == "ApplicationFrameWindow" ||
                className == "Windows.UI.Core.CoreWindow")
                return true;

            var title = GetWindowTitle(hWnd);

            result.Add(new WindowInfo(hWnd, title, rect));
            return true;
        }, IntPtr.Zero);

        return result;
    }
    public static bool HasAnyFullscreenWindow()
    {
        bool found = false;

        EnumWindows((hWnd, _) =>
        {
            if (found)
                return false;

            if (!IsWindowVisible(hWnd) || IsIconic(hWnd))
                return true;

            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                return true;

            if (!GetWindowRect(hWnd, out var windowRect))
                return true;

            if (windowRect.Width <= 0 || windowRect.Height <= 0)
                return true;

            if (IsCloaked(hWnd))
                return true;

            if ((exStyle & WS_EX_LAYERED) != 0 &&
                (exStyle & WS_EX_TRANSPARENT) != 0)
                return true;

            IntPtr monitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
                return true;

            var mi = new MONITORINFO();
            mi.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();

            if (!GetMonitorInfo(monitor, ref mi))
                return true;

            var monitorRect = mi.rcMonitor;

            const int tolerance = 2;

            bool fullscreen =
                Math.Abs(windowRect.Left - monitorRect.Left) <= tolerance &&
                Math.Abs(windowRect.Top - monitorRect.Top) <= tolerance &&
                Math.Abs(windowRect.Right - monitorRect.Right) <= tolerance &&
                Math.Abs(windowRect.Bottom - monitorRect.Bottom) <= tolerance;

            if (fullscreen)
            {
                found = true;
                return false;
            }

            return true;
        }, IntPtr.Zero);
        return found;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out int pvAttribute,
        int cbAttribute);

    private const int DWMWA_CLOAKED = 14;

    private static bool IsCloaked(IntPtr hwnd)
    {
        int cloaked = 0;
        DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
        return cloaked != 0;
    }
    const int WS_EX_LAYERED = 0x00080000;
    const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    static string GetWindowTitle(IntPtr hWnd)
    {
        int len = GetWindowTextLength(hWnd);
        var sb = new StringBuilder(len + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    static string GetWindowClass(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }
}