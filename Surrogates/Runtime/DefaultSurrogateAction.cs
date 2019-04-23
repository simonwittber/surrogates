using System.Reflection;
using UnityEngine;

namespace Surrogates
{
    public class DefaultSurrogateAction : ISurrogateAction
    {
        MethodInfo _methodInfo;
        Component _component;

        public DefaultSurrogateAction(Component target, MethodInfo propertyInfo)
        {
            _component = target;
            _methodInfo = propertyInfo;
        }

        public void Invoke()
        {
            if (_component != null)
                _methodInfo.Invoke(_component, null);
        }

        public void SetComponent(Component component)
        {
            _component = component;
        }
    }
}