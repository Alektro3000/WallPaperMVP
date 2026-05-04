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
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out int pvAttribute,
        int cbAttribute);
    private const int DWMWA_CLOAKED = 14;

    private static bool IsCloaked(IntPtr hwnd)
    {
        DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int));
        return cloaked != 0;
    }

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

            // Windows on other virtual desktops are commonly cloaked by DWM.
            if (IsCloaked(hWnd))
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
    public static bool IsAnyWindowFullscreen()
    {
        return GetWindows().Any(wind =>
        {
            
            IntPtr monitor = MonitorFromWindow(wind.Handle, MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO();
            mi.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();
            if (!GetMonitorInfo(monitor, ref mi))
                return false;

            var monitorRect = mi.rcMonitor;
            const int tolerance = -2;

            return
                monitorRect.Left - wind.Rect.Left >= tolerance &&
                monitorRect.Top - wind.Rect.Top >= tolerance &&
                wind.Rect.Right - monitorRect.Right >= tolerance &&
                wind.Rect.Bottom - monitorRect.Bottom >= tolerance;
        }

        );


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


    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);


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
