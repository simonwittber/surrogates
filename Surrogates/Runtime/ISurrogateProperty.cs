using UnityEngine;

namespace Surrogates
{
    public interface ISurrogateProperty
    {
        void SetComponent(Component component);
    }

    public interface ISurrogateProperty<T> : ISurrogateProperty
    {
        T Get();
        void Set(T value);
    }
}