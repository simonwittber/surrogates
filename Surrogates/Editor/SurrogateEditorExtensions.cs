using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Surrogates
{

    public class SurrogateEditorExtensions
    {
        static string[] blackListesAssemblies = new string[]  {
            "UnityEngine.GoogleAudioSpatializer, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
            "UnityEngine.VRModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Unity.Rendering, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Unity.Scenes, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
        };

        public static void BuildEverything()
        {
            var count = 0;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (blackListesAssemblies.Contains(asm.FullName)) continue;

                foreach (var type in asm.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(Component)))
                    {
                        count += AddClassToRegister(type);
                    }
                }
            }
            Debug.Log($"{count} classed generated.");
            SurrogateCompiler.Save();
            AssetDatabase.Refresh();
        }

        public static int AddClassToRegister(Type type)
        {
            var count = 0;
            foreach (var p in type.GetProperties())
            {
                if (!IsSupportedType(p.PropertyType)) continue;
                if (p.DeclaringType != type) continue;
                if (p.DeclaringType.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                if (p.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                SurrogateCompiler.CreateProperty(p);
                // count++;
            }

            foreach (var m in type.GetMethods())
            {
                if (IsSupportedMethod(type, m))
                {
                    SurrogateCompiler.CreateAction(m);
                    count++;
                }
            }

            foreach (var f in type.GetFields())
            {
                if (!IsSupportedType(f.FieldType)) continue;
                if (f.DeclaringType != type) continue;
                if (f.DeclaringType.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                if (f.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                SurrogateCompiler.CreateField(f);
                // count++;
            }
            return count;
        }

        public static bool IsSupportedMethod(Type type, MethodInfo m)
        {
            if (!(m.ReturnType == typeof(bool) || m.ReturnType == typeof(void))) return false;
            if (m.ContainsGenericParameters) return false;
            foreach (var p in m.GetParameters())
            {
                if (!IsSupportedType(p.ParameterType)) return false;
                if (p.IsOut) return false;
            }
            if (m.DeclaringType != type) return false;
            if (m.IsSpecialName) return false;
            if (m.DeclaringType.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) return false;
            if (m.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) return false;
            return true;
        }

        public static bool IsSupportedType(Type t)
        {
            if (t.IsArray || t.IsByRef || t.IsNotPublic || t.IsInterface || t.IsPointer)
                return false;
            if (t.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0)
                return false;
            if (t == typeof(string))
                return true;
            if (t == typeof(int))
                return true;
            if (t == typeof(bool))
                return true;
            if (t == typeof(float))
                return true;
            if (t == typeof(Vector3))
                return true;
            if (t == typeof(Vector2))
                return true;
            if (t == typeof(Vector4))
                return true;
            if (t == typeof(Color))
                return true;
            if (t.IsEnum)
                return true;
            if (t == typeof(LayerMask))
                return true;
            if (t.IsSubclassOf(typeof(UnityEngine.Object)))
                return true;
            return false;
        }

        public static string[] GetParameterTypeNames(MethodInfo mi)
        {
            return (from i in mi.GetParameters() select i.ParameterType.AssemblyQualifiedName).ToArray();
        }

        public static string GetNiceName(System.Type type, MethodInfo mi)
        {
            var name = mi.Name;
            if (mi.IsSpecialName && mi.Name.StartsWith("set_"))
                name = $"Set {name.Substring(4)} =";
            return $"{type.Name}.{name} ({string.Join(", ", from i in mi.GetParameters() select i.Name)})";
        }

        public static string GetClassName(Type type, MethodInfo mi)
        {
            var niceAssemblyName = string.Join("", from i in type.Assembly.GetName().Name where char.IsLetterOrDigit(i) select i);
            return $"{niceAssemblyName}_{type.Name}_{mi.Name}_" + string.Join("_", (from i in mi.GetParameters() select i.ParameterType.Name));
        }
    }

}