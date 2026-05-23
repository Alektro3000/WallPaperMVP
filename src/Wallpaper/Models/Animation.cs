

using System.Numerics;

namespace Models;

public class Animation
{
    public string Name;

    public float TotalTime;

    public float animationDelta = 0;
    public List<AnimationNode> AnimationNodes;

    public Animation(string name, float totalTime, List<AnimationNode> animationNodes)
    {
        Name = name;
        TotalTime = totalTime;
        AnimationNodes = animationNodes;
    }

}

public class AnimationNode
{
    public Node Node;
    public List<LinearKey<Vector3>> Translations = new();
    public List<LinearKey<Quaternion>> Rotations = new();
    public List<LinearKey<Vector3>> Scales = new();

    public void UpdateTransform(float time)
    {
        Node.LocalTransform = new AffineTransform(
            SampleVector(Translations, time, Node.LocalTransform.Translation),
            SampleQuaternion(Rotations, time, Node.LocalTransform.Rotation),
            SampleVector(Scales, time, Node.LocalTransform.Scale));
    }

    public static Vector3 SampleVector(List<LinearKey<Vector3>> keys, float time, Vector3 fallback)
    {
        return LinearKey<Vector3>.SampleLinear(keys, time, (a,b,t) => Vector3.Lerp(a,b,t)) ?? fallback;

    }

    public static Quaternion SampleQuaternion(List<LinearKey<Quaternion>> keys, float time, Quaternion fallback)
    {
        return LinearKey<Quaternion>.SampleLinear(keys, time, (a,b,t) => Quaternion.Slerp(a,b,t)) ?? fallback;
    }
}

public readonly record struct LinearKey<T>(float Time, T Value) where T : struct, IEquatable<T>
{
    public static T? SampleLinear(
        List<LinearKey<T> > keys, float time,
        Func<T, T, float, T> lerp)
    {
        if (keys.Count == 0)
            return null;

        if (time <= keys[0].Time)
            return keys[0].Value;

        if (time >= keys[^1].Time)
            return keys[^1].Value;

        for (int i = 0; i < keys.Count - 1; i++)
        {
            var a = keys[i];
            var b = keys[i + 1];

            if (time < a.Time || time > b.Time)
                continue;

            float t = (time - a.Time) / (b.Time - a.Time);
            return lerp(a.Value,b.Value ,t);
        }

        return null;
    }
}