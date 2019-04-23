using UnityEngine;

namespace Surrogates
{
    public interface ISurrogateAction : ISurrogate
    {
        void Invoke();
    }
}