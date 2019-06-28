using UnityEngine;

namespace Surrogates
{
    public interface ISurrogateAction : ISurrogate
    {
        void Invoke();
    }

    public interface ISurrogateAction<T> : ISurrogate
    {
        T Invoke();
    }
}