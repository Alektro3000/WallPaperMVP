
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Mathematics;

namespace Particles.Shared.Field;

[StructLayout(LayoutKind.Sequential)]
public struct WindowFieldDescription
{
    public Vector2 PrevMin;
    public Vector2 PrevMax;
    public Vector2 CurrMin;
    public Vector2 CurrMax;
    public Vector2 ExtendedMin;
    public Vector2 ExtendedMax;
    public WindowFieldDescription(){}
    public WindowFieldDescription(WindowEnumerator.RECT rect, WindowEnumerator.RECT? prevRect, Vector2 Scaling, Vector2 ScreenSize, FieldWindowSettings settings) : this()
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

        ExtendedMin = CurrMin;
        ExtendedMax = CurrMax;


        //Vector2 size = 
        Vector2 fieldSize = ScreenSize * Scaling;
        Vector2 extend = (CurrMax-CurrMin) * settings.BorderExtendFactor;
        float transition = settings.BorderTransitionDistance;

        // left
        {
            float dist = CurrMin.X;
            float t = BorderExtendFactor(dist, transition);
            ExtendedMin.X -= extend.Y * t;
        }

        // right
        {
            float dist = fieldSize.X - CurrMax.X;
            float t = BorderExtendFactor(dist, transition);
            ExtendedMax.X += extend.Y * t;
        }

        // top
        {
            float dist = CurrMin.Y;
            float t = BorderExtendFactor(dist, transition);
            ExtendedMin.Y -= extend.X * t;
        }

        // bottom
        {
            float dist = fieldSize.Y - CurrMax.Y;
            float t = BorderExtendFactor(dist, transition);
            ExtendedMax.Y += extend.X * t;
        }

    }

    static float BorderExtendFactor(float distanceToBorder, float transition)
    {
        float t = (transition - distanceToBorder) / transition;
        t = Math.Clamp(t, 0.0f, 1.0f);

        // smoothstep (optional, nicer transition)
        return t * t * (3.0f - 2.0f * t);
    }
    
}

[InlineArray(32)]
public struct WindowFieldDescriptionBuffer
{
    WindowFieldDescription windowFieldDescription;
}

[StructLayout(LayoutKind.Sequential)]
public struct FieldConstantBuffer
{
    public DebugSettings debugSettings = new DebugSettings();
    public FieldGpuSettings fieldSettings = new FieldGpuSettings();
    public uint WindowsCount;
    Vector3 Padding;

    public FieldConstantBuffer()
    {
    }
}
