using System.Reflection;
using UnityEngine;

namespace Surrogates
{
    public class DefaultSurrogateProperty<T> : ISurrogateProperty<T>
    {
        PropertyInfo _propertyInfo;
        Component _component;

        public DefaultSurrogateProperty(Component target, PropertyInfo propertyInfo)
        {
            _component = target;
            _propertyInfo = propertyInfo;
        }

        public T Get()
        {
            if (_component == null) return default(T);
            return (T)_propertyInfo.GetValue(_component, null);
        }

        public void Set(T value)
        {
            if (_component != null)
                _propertyInfo.SetValue(_component, value, null);
        }

        public void SetComponent(Component component)
        {
            _component = component;
        }
    }
}