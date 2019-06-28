using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Scripting;
using UnityEditor;

namespace Surrogates
{
    public static class SurrogateCompilerCS
    {
        static string assemblyName = "Xyzzy.Surrogate";
        static string assemblyFileName;

        static Dictionary<string, string> propertyIndex;
        static Dictionary<string, string> methodIndex;
        static Dictionary<string, string> fieldIndex;

        static SurrogateCompilerCS()
        {
            propertyIndex = new Dictionary<string, string>();
            fieldIndex = new Dictionary<string, string>();
            methodIndex = new Dictionary<string, string>();
        }

        public static void Save()
        {
            var assetPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(SurrogateRegister.Instance));

            foreach (var kv in propertyIndex)
            {
                var path = System.IO.Path.Combine(assetPath, kv.Key + ".cs");
                using (var output = System.IO.File.OpenWrite(path))
                {
                    using (var tw = new System.IO.StreamWriter(output))
                    {
                        tw.Write(kv.Value);
                        tw.Flush();
                    }

                }
            }

        }

        public static string CreateField(FieldInfo fi)
        {
            var template = $@"
    [System.Serializable]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public class {fi.DeclaringType.Name}_{fi.Name} : ISurrogateProperty<{fi.FieldType.Name}> {{
        Component component;
        public void SetComponent(Component component) => this.component = component;
        public {fi.FieldType.Name} Get() => component.{fi.Name};
        public {fi.FieldType.Name} Set({fi.FieldType.Name} value) => component.{fi.Name} = value;
    }}
            ";
            // fieldIndex[fi] = template;
            return template;
        }


        public static string CreateProperty(PropertyInfo pi)
        {
            var className = $"P_{(uint)pi.DeclaringType.GetHashCode()}_{(uint)pi.GetHashCode()}";
            var template = $@"
using UnityEngine;
namespace Surrogates {{
    [System.Serializable]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public class {className} : ISurrogateProperty<{pi.PropertyType.Name}> {{
        Component component;
        public void SetComponent(Component component) => this.component = component;
        public {pi.PropertyType.Name} Get() => component.{pi.Name};
        public {pi.PropertyType.Name} Set({pi.PropertyType.Name} value) => component.{pi.Name} = value;
    }}
}}
";
            propertyIndex[className] = template;
            return template;
        }

        public static string CreateAction(MethodInfo mi)
        {
            var className = $"{mi.DeclaringType.Name}_{mi.Name}";
            var template = $@"
    [System.Serializable]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public class {className} : ISurrogateAction {{
        Component component;
        public void SetComponent(Component component) => this.component = component;
        public void Invoke() => component.{mi.Name}();
    }}
            ";
            // methodIndex[mi] = template;
            return template;
        }

        public static string CreateAction(MethodInfo mi, ParameterInfo[] parameters)
        {
            var className = $"{mi.DeclaringType.Name}_{mi.Name}_{(uint)string.Join("_", (from i in parameters select i.ParameterType.Name)).GetHashCode()}";
            var fields = string.Join("            \n", (from i in parameters select $"public {i.ParameterType.Name} {i.Name};"));
            var arguments = string.Join(", ", (from i in parameters select $"{i.Name}"));
            var template = $@"
    [System.Serializable]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public class {className} : ISurrogateAction {{
        Component component;
        {fields}
        public void SetComponent(Component component) => this.component = component;
        public void Invoke() => component.{mi.Name}({arguments});
    }}
            ";
            // methodIndex[mi] = template;
            return template;
        }

        public static string CreateAction(Type returnType, MethodInfo mi, ParameterInfo[] parameters)
        {
            var className = $"{mi.DeclaringType.Name}_{mi.Name}_{(uint)string.Join("_", (from i in parameters select i.ParameterType.Name)).GetHashCode()}";
            var fields = string.Join("            \n", (from i in parameters select $"public {i.ParameterType.Name} {i.Name};"));
            var arguments = string.Join(", ", (from i in parameters select $"{i.Name}"));
            var template = $@"
    [System.Serializable]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public class {className} : ISurrogateAction<{returnType.Name}> {{
        Component component;
        {fields}
        public void SetComponent(Component component) => this.component = component;
        public {returnType.Name} Invoke() => component.{mi.Name}({arguments});
    }}
            ";
            // methodIndex[mi] = template;
            return template;
        }

    }
}
