
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Mathematics;

namespace Particles.Shared.Field;

public struct WindowFieldDescription
{
    public Vector2 PrevMin;
    public Vector2 PrevMax;
    public Vector2 CurrMin;
    public Vector2 CurrMax;
    public WindowFieldDescription(WindowEnumerator.RECT rect, WindowEnumerator.RECT? prevRect, Vector2 Scaling) : this()
    {
        CurrMin = new Vector2(rect.Left, rect.Top) * Scaling;
        CurrMax = new Vector2(rect.Right, rect.Bottom) * Scaling;

        if (prevRect.HasValue)
        {
            var prev = prevRect.Value;
            PrevMin = new Vector2(prev.Left, prev.Top);
            PrevMax = new Vector2(prev.Right, prev.Bottom);
        }
        else
        {
            PrevMin = CurrMin;
            PrevMax = CurrMax;
        }
        PrevMin *= Scaling;
        PrevMax *= Scaling;
    }
}

[InlineArray(32)]
public struct WindowFieldDescriptionArray
{
    private WindowFieldDescription _element0;
}

[StructLayout(LayoutKind.Sequential)]
public struct FieldConstantBuffer
{
    public WindowFieldDescriptionArray Descriptors;
    public uint ScreenWidth;
    public uint ScreenHeight;
    public uint windowsCount;
}