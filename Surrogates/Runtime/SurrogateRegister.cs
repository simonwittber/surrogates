using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UnityEngine;

namespace Surrogates
{
    [CreateAssetMenu]
    public class SurrogateRegister : ScriptableObject, ISerializationCallbackReceiver
    {
        public int count;
        public int size;
        public static bool isDirty;
        public static Dictionary<PropertyInfo, Type> propertyIndex = new Dictionary<PropertyInfo, Type>();
        public static Dictionary<FieldInfo, Type> fieldIndex = new Dictionary<FieldInfo, Type>();
        public static Dictionary<MethodInfo, Type> methodIndex = new Dictionary<MethodInfo, Type>();
        public static SurrogateRegister Instance { get; private set; }

        public static ISurrogateProperty<T> GetSurrogateProperty<T>(Component target, string propertyName)
        {
            var propertyInfo = target.GetType().GetProperty(propertyName);
            Type type;
            if (!propertyIndex.TryGetValue(propertyInfo, out type))
                propertyIndex[propertyInfo] = null;
            if (type == null)
            {
                isDirty = true;
                return new DefaultSurrogateProperty<T>(target, propertyInfo);
            }
            var instance = (ISurrogateProperty<T>)System.Activator.CreateInstance(type);
            instance.SetComponent(target);
            return instance;
        }

        public static ISurrogateProperty<T> GetSurrogateField<T>(Component target, string fieldName)
        {
            var fieldInfo = target.GetType().GetField(fieldName);
            Type type;
            if (!fieldIndex.TryGetValue(fieldInfo, out type))
                fieldIndex[fieldInfo] = null;
            if (type == null)
            {
                isDirty = true;
                return new DefaultSurrogateField<T>(target, fieldInfo);
            }
            var instance = (ISurrogateProperty<T>)System.Activator.CreateInstance(type);
            instance.SetComponent(target);
            return instance;
        }

        public static ISurrogateAction GetSurrogateAction(Type type, string methodName)
        {
            var methodInfo = type.GetMethod(methodName);
            if (methodInfo == null) throw new ArgumentException("Method Not Found:" + methodName);
            Type t;
            if (!methodIndex.TryGetValue(methodInfo, out t))
                methodIndex[methodInfo] = null;
            if (t == null)
            {
                isDirty = true;
                return new DefaultSurrogateAction(type, methodInfo);
            }
            var instance = (ISurrogateAction)System.Activator.CreateInstance(t);
            return instance;
        }

        public static void SetSurrogate(PropertyInfo propertyInfo, Type type)
        {
            if (propertyInfo == null || type == null) return;
            propertyIndex[propertyInfo] = type;
        }

        public static ISurrogateAction GetSurrogateAction(Component target, string methodName)
        {
            var instance = GetSurrogateAction(target.GetType(), methodName);
            instance.SetComponent(target);
            return instance;
        }

        public static void SetSurrogate(MethodInfo methodInfo, Type type)
        {
            if (methodInfo == null || type == null) return;
            methodIndex[methodInfo] = type;
        }

        public void OnBeforeSerialize()
        {
            var bf = new BinaryFormatter();
            var s = new SerializationContainer() { propertyIndex = propertyIndex, methodIndex = methodIndex };
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, s);
                serializationBytes = ms.ToArray();
            }
            size = serializationBytes.Length;
            count = propertyIndex.Count + methodIndex.Count;
        }

        public void OnAfterDeserialize()
        {
            if (serializationBytes == null) return;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream(serializationBytes))
            {
                var s = (SerializationContainer)bf.Deserialize(ms);
                methodIndex = s.methodIndex;
                propertyIndex = s.propertyIndex;
            }
            Instance = this;
        }

        [Serializable]
        struct SerializationContainer
        {
            public Dictionary<PropertyInfo, Type> propertyIndex;
            public Dictionary<MethodInfo, Type> methodIndex;
        }

        [SerializeField] byte[] serializationBytes;
    }
}