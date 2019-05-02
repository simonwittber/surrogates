using System.Reflection;
using UnityEngine;

namespace Surrogates
{
    public class DefaultSurrogateField<T> : ISurrogateProperty<T>
    {
        FieldInfo _fieldInfo;
        Component _component;

        public DefaultSurrogateField(Component target, FieldInfo fieldInfo)
        {
            _component = target;
            _fieldInfo = fieldInfo;
        }

        public T Get()
        {
            if (_component == null) return default(T);
            return (T)_fieldInfo.GetValue(_component);
        }

        public void Set(T value)
        {
            if (_component != null)
                _fieldInfo.SetValue(_component, value);
        }

        public void SetComponent(Component component)
        {
            _component = component;
        }
    }
}