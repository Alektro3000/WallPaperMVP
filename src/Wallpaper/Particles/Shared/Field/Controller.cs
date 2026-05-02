

using System.Numerics;
using Particles.Settings;
using Renderer.Core;
using Renderer.FrameManagement;

namespace Particles.Shared.Field;

public class Controller(Buffers buffers) : IConstantUpdater
{
    public List<WindowEnumerator.WindowInfo> previousWindows = [];
    public void UpdateConstants(FrameResource currentResource, SystemSettings systemSettings)
    {
        UpdateConstant(currentResource, ref currentResource.GetBufferConstantRef<FieldConstantBuffer>(buffers.fieldKey));
    }
    private void UpdateConstant(FrameResource currentResource, ref FieldConstantBuffer constant)
    {
        constant.ScreenWidth = (uint)currentResource.frameMetric.width;
        constant.ScreenHeight = (uint)currentResource.frameMetric.height; 
        var screenHeight = currentResource.frameMetric.height;
        var windows = WindowEnumerator.GetWindows()
            .OrderByDescending(x => x.Rect.Width * x.Rect.Height)
            .Take(32)
            .Select(w =>
            {
                var r = w.Rect;

                // Convert from Win32 top-left origin to bottom-left origin
                var flipped = new WindowEnumerator.RECT
                {
                    Left = r.Left,
                    Right = r.Right,
                    Top = screenHeight - r.Bottom,
                    Bottom = screenHeight - r.Top
                };

                return new WindowEnumerator.WindowInfo(w.Handle, w.Name, flipped);
            })
            .ToList();
        constant.windowsCount = (uint)windows.Count;

        Vector2 scaling = new Vector2(
            (float)Buffers.width  / currentResource.frameMetric.width ,
            (float)Buffers.height / currentResource.frameMetric.height);

        var shaderWindows = windows
            .Select(window =>
                new WindowFieldDescription(
                    window.Rect,
                    previousWindows.Find(x=>x.Handle == window.Handle)?.Rect,
                    scaling
                )).ToArray();
        for(int i = 0; i < constant.windowsCount; i++)
            constant.Descriptors[i] = shaderWindows[i];
        previousWindows = windows;
    }

}