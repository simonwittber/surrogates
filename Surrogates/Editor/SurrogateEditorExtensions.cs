using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Surrogates
{
    public class SurrogateEditorExtensions
    {
        static string[] blackListesAssemblies = new string[]  {
            "UnityEngine.GoogleAudioSpatializer, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
            "UnityEngine.VRModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
        };

        [MenuItem("Assets/Create/Surrogate Assembly")]
        static void BuildSurrogateDLL()
        {
            Rebuild();
            SurrogateCompiler.Save();
            AssetDatabase.Refresh();
        }

        public static void Rebuild()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (blackListesAssemblies.Contains(asm.FullName)) continue;

                foreach (var type in asm.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(Component)))
                    {
                        AddClassToRegister(type);
                    }
                }
            }
        }

        static void AddClassToRegister(Type type)
        {
            foreach (var p in type.GetProperties())
            {
                if (!IsSupportedType(p.PropertyType)) continue;
                if (p.DeclaringType != type) continue;
                if (p.DeclaringType.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                if (p.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                SurrogateCompiler.CreateProperty(p);
            }

            foreach (var m in type.GetMethods())
            {
                if (m.GetParameters().Length > 0) continue;
                if (m.DeclaringType != type) continue;
                if (m.IsSpecialName) continue;
                if (m.DeclaringType.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                if (m.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                SurrogateCompiler.CreateAction(m);
            }
        }

        static bool IsSupportedType(Type t)
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

    }

}