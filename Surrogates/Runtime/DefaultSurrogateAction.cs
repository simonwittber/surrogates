using System;
using System.Reflection;
using UnityEngine;

namespace Surrogates
{
    public class DefaultSurrogateAction : ISurrogateAction
    {
        public MethodInfo methodInfo;
        public Component component;
        public GameObject gameObject;
        public object[] args = new object[0];
        public Type type;

        public DefaultSurrogateAction(Component target, MethodInfo propertyInfo)
        {
            component = target;
            methodInfo = propertyInfo;
        }

        public DefaultSurrogateAction(Type type, MethodInfo methodInfo)
        {
            this.type = type;
            this.methodInfo = methodInfo;
        }

        public void Invoke()
        {
            if (component != null)
                methodInfo.Invoke(component, args);
            else if (gameObject != null)
                methodInfo.Invoke(gameObject, args);
            else
                methodInfo.Invoke(null, args);
        }

        public void SetComponent(Component component)
        {
            this.component = component;
        }
    }
}