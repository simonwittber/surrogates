namespace Surrogates
{

    public interface ISurrogateProperty<T> : ISurrogate
    {
        T Get();
        void Set(T value);
    }
}