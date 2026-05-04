

namespace Renderer.Resources;
public interface IConstantBufferKey
{
    internal int Key { get; }
}
public readonly struct ConstantBufferKey<T> : IConstantBufferKey where T: unmanaged 
{
    internal readonly int Key;

    internal ConstantBufferKey(int index)
    {
        Key = index;
    }

    int IConstantBufferKey.Key => Key;
}