using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DifferentMethods.Extensions.Serialization;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Surrogates
{
    [ExecuteAlways]
    [CreateAssetMenu]
    public class SurrogateRegister : ScriptableObject, ISerializationCallbackReceiver
    {
        public Dictionary<PropertyInfo, string> propertyIndex = new Dictionary<PropertyInfo, string>();
        public Dictionary<FieldInfo, string> fieldIndex = new Dictionary<FieldInfo, string>();
        public Dictionary<MethodInfo, string> methodIndex = new Dictionary<MethodInfo, string>();

        public HashSet<MethodInfo> missingMethods = new HashSet<MethodInfo>();
        public HashSet<FieldInfo> missingFields = new HashSet<FieldInfo>();
        public HashSet<PropertyInfo> missingProperties = new HashSet<PropertyInfo>();

        [SerializeField] byte[] serializationBytes;

        static SurrogateRegister _instance;
        public static SurrogateRegister Instance
        {
            get
            {
                if (!_instance)
                    _instance = Resources.FindObjectsOfTypeAll<SurrogateRegister>().FirstOrDefault();
                return _instance;
            }
        }

        public ISurrogateProperty<T> GetSurrogateField<T>(Component target, FieldInfo fieldInfo)
        {
            string type;
            if (!fieldIndex.TryGetValue(fieldInfo, out type))
                fieldIndex[fieldInfo] = null;
            if (type == null)
            {
                UnityEngine.Debug.Log("Missing: " + fieldInfo.DeclaringType.Name);
                Instance.missingFields.Add(fieldInfo);
                return new DefaultSurrogateField<T>(target, fieldInfo);
            }
            var surrogateType = Type.GetType(type);
            if (surrogateType == null)
            {
                fieldIndex[fieldInfo] = null;
                UnityEngine.Debug.Log("Missing: " + fieldInfo.DeclaringType.Name);
                Instance.missingFields.Add(fieldInfo);
                return new DefaultSurrogateField<T>(target, fieldInfo);
            }
            var instance = (ISurrogateProperty<T>)System.Activator.CreateInstance(surrogateType);
            instance.SetComponent(target);
            return instance;
        }

        public ISurrogateProperty<T> GetSurrogateProperty<T>(Component target, PropertyInfo propertyInfo)
        {
            string type;
            if (!propertyIndex.TryGetValue(propertyInfo, out type))
                propertyIndex[propertyInfo] = null;
            if (type == null)
            {
                UnityEngine.Debug.Log("Missing: " + propertyInfo.DeclaringType.Name);
                Instance.missingProperties.Add(propertyInfo);
                return new DefaultSurrogateProperty<T>(target, propertyInfo);
            }
            var surrogateType = Type.GetType(type);
            if (surrogateType == null)
            {
                propertyIndex[propertyInfo] = null;
                UnityEngine.Debug.Log("Missing: " + propertyInfo.DeclaringType.Name);
                Instance.missingProperties.Add(propertyInfo);
                return new DefaultSurrogateProperty<T>(target, propertyInfo);
            }
            var instance = (ISurrogateProperty<T>)System.Activator.CreateInstance(surrogateType);
            instance.SetComponent(target);
            return instance;
        }

        public ISurrogateAction GetSurrogateAction(MethodInfo methodInfo)
        {
            if (methodInfo == null) return null;
            string type;
            if (!methodIndex.TryGetValue(methodInfo, out type))
                methodIndex[methodInfo] = null;
            if (type == null)
            {
                UnityEngine.Debug.Log($"Missing: {methodInfo.DeclaringType.Name} {methodInfo.Name}");
                Instance.missingMethods.Add(methodInfo);
                return new DefaultSurrogateAction(methodInfo.DeclaringType, methodInfo);
            }
            var surrogateType = Type.GetType(type);
            if (surrogateType == null)
            {
                methodIndex[methodInfo] = null;
                UnityEngine.Debug.Log($"Missing: {methodInfo.DeclaringType.Name} {methodInfo.Name}");
                Instance.missingMethods.Add(methodInfo);
                return new DefaultSurrogateAction(methodInfo.DeclaringType, methodInfo);
            }
            var instance = (ISurrogateAction)System.Activator.CreateInstance(surrogateType);
            return instance;
        }

        public ISurrogateAction GetSurrogateAction(Component target, MethodInfo methodInfo)
        {
            var instance = GetSurrogateAction(methodInfo);
            if (instance == null) return null;
            instance.SetComponent(target);
            return instance;
        }

        public void SetSurrogate(PropertyInfo propertyInfo, Type type)
        {
            if (propertyInfo == null || type == null)
            {
                UnityEngine.Debug.LogError($"Invalid Register Call: {propertyInfo} {type}");
                return;
            }
            propertyIndex[propertyInfo] = type.AssemblyQualifiedName;
        }

        public void SetSurrogate(MethodInfo methodInfo, Type type)
        {
            if (methodInfo == null || type == null)
            {
                UnityEngine.Debug.LogError($"Invalid Register Call: {methodInfo} {type}");
                return;
            }
            methodIndex[methodInfo] = type.AssemblyQualifiedName;
        }

        public void SetSurrogate(FieldInfo fieldInfo, Type type)
        {
            if (fieldInfo == null || type == null)
            {
                UnityEngine.Debug.LogError($"Invalid Register Call: {fieldInfo} {type}");
                return;
            }
            fieldIndex[fieldInfo] = type.AssemblyQualifiedName;
        }

        public void OnBeforeSerialize()
        {
            var sc = new SerializationContainer()
            {
                methods = missingMethods.ToList(),
                fields = missingFields.ToList(),
                properties = missingProperties.ToList(),
                methodIndex = methodIndex,
                fieldIndex = fieldIndex,
                propertyIndex = propertyIndex
            };
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                bf.Serialize(ms, sc);
                serializationBytes = ms.ToArray();
            }
        }

        public void OnAfterDeserialize()
        {
            using (var ms = new MemoryStream(serializationBytes))
            {
                var bf = new BinaryFormatter();
                var sc = (SerializationContainer)bf.Deserialize(ms);
                missingFields = new HashSet<FieldInfo>(sc.fields);
                missingMethods = new HashSet<MethodInfo>(sc.methods);
                missingProperties = new HashSet<PropertyInfo>(sc.properties);
                methodIndex = new Dictionary<MethodInfo, string>(sc.methodIndex);
                fieldIndex = new Dictionary<FieldInfo, string>(sc.fieldIndex);
                propertyIndex = new Dictionary<PropertyInfo, string>(sc.propertyIndex);
            }
        }

        [Serializable]
        struct SerializationContainer
        {
            public List<MethodInfo> methods;
            public List<FieldInfo> fields;
            public List<PropertyInfo> properties;
            public Dictionary<PropertyInfo, string> propertyIndex;
            public Dictionary<FieldInfo, string> fieldIndex;
            public Dictionary<MethodInfo, string> methodIndex;
        }

    }
}