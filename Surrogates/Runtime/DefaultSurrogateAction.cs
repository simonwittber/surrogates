using System;
using System.Reflection;
using UnityEngine;

namespace Surrogates
{
    public class DefaultSurrogateAction : ISurrogateAction
    {
        MethodInfo _methodInfo;
        Component _component;
        Type _type;

        public DefaultSurrogateAction(Component target, MethodInfo propertyInfo)
        {
            _component = target;
            _methodInfo = propertyInfo;
        }

        public DefaultSurrogateAction(Type type, MethodInfo methodInfo)
        {
            _type = type;
            _methodInfo = methodInfo;
        }

        public void Invoke()
        {
            if (_component != null)
                _methodInfo.Invoke(_component, null);
            else
                _methodInfo.Invoke(null, null);
        }

        public void SetComponent(Component component)
        {
            _component = component;
        }
    }
}