

using System.Numerics;
using System.Runtime.InteropServices;
using Particles.Core;
using Particles.Settings;
using Renderer.Core;
using Renderer.FrameManagement;

namespace Particles.Shared.Field;

public class Controller(Buffers buffers) : IConstantUpdater, IParticleSystemFor<Settings>
{
    public List<WindowEnumerator.WindowInfo> previousWindows = [];
    public void UpdateConstants(FrameResource currentResource, SystemSettings systemSettings)
    {
        UpdateConstant(currentResource, 
        ref currentResource.GetBufferConstantRef(buffers.fieldKey), 
        ref currentResource.GetBufferConstantRef(buffers.windowDescriptors), 
        systemSettings);
    }
    private void UpdateConstant(FrameResource currentResource, 
    ref FieldConstantBuffer constant, 
    ref WindowFieldDescriptionBuffer descriptionBuffer, SystemSettings systemSettings)
    {
        constant.debugSettings = systemSettings.GetSettings<Settings>().gpuSettings;
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
        constant.WindowsCount = (uint)windows.Count;

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
                
        for(int i = 0; i < constant.WindowsCount; i++)
            descriptionBuffer[i] = shaderWindows[i];
        
        previousWindows = windows;
    }

}