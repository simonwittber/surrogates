using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace Surrogates
{
    public static class SurrogateRegister
    {
        static Dictionary<PropertyInfo, Type> propertyIndex = new Dictionary<PropertyInfo, Type>();

        public static ISurrogateProperty<T> GetSurrogate<T>(Component target, string propertyName)
        {
            var propertyInfo = target.GetType().GetProperty(propertyName);
            Type type;
            if (!propertyIndex.TryGetValue(propertyInfo, out type))
                return new DefaultSurrogateProperty<T>(target, propertyInfo);
            var instance = (ISurrogateProperty<T>)System.Activator.CreateInstance(type);
            instance.SetComponent(target);
            return instance;
        }

        public static void SetSurrogate(PropertyInfo propertyInfo, Type type)
        {
            if (propertyInfo == null || type == null) return;
            propertyIndex[propertyInfo] = type;
        }
    }
}