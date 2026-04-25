

using System.Numerics;
using Vortice.Direct3D12;

public class FieldUpdater(FieldBuffers buffers) : IConstantUpdater
{
    public List<WindowEnumerator.WindowInfo> previousWindows = [];
    public void UpdateConstants(FrameResource currentResource, ParticleSystems.SystemSettings systemSettings)
    {
        UpdateConstant(currentResource, ref currentResource.GetBufferConstantRef<FieldConstantBuffer>(buffers.fieldKey));
    }
    private void UpdateConstant(FrameResource currentResource, ref FieldConstantBuffer constant)
    {
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
            (float)FieldBuffers.width  / currentResource.frameMetric.width ,
            (float)FieldBuffers.height / currentResource.frameMetric.height);

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