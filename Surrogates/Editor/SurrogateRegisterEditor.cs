using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Surrogates
{
    [CustomEditor(typeof(SurrogateRegister))]
    public class SurrogateRegisterEditor : Editor
    {
        void Build()
        {
            var sr = target as SurrogateRegister;

            foreach (var mi in sr.methodIndex.Keys.ToArray())
                sr.methodIndex[mi] = SurrogateCompiler.CreateAction(mi).AssemblyQualifiedName;
            foreach (var fi in sr.fieldIndex.Keys.ToArray())
                sr.fieldIndex[fi] = SurrogateCompiler.CreateField(fi).AssemblyQualifiedName;
            foreach (var pi in sr.propertyIndex.Keys.ToArray())
                sr.propertyIndex[pi] = SurrogateCompiler.CreateProperty(pi).AssemblyQualifiedName;

            sr.missingFields.Clear();
            sr.missingMethods.Clear();
            sr.missingProperties.Clear();
            EditorUtility.SetDirty(sr);
            SurrogateCompiler.Save();
            AssetDatabase.Refresh();
        }

        public override void OnInspectorGUI()
        {
            var sr = target as SurrogateRegister;
            if (GUILayout.Button("Bake Reflected Methods"))
            {
                Build();
            }
            GUILayout.Label("Reflected Methods");
            GUILayout.BeginVertical();
            foreach (var i in sr.missingMethods)
                GUILayout.Label($" - {i.DeclaringType.Name}.{i.Name}");
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.Label("Reflected Fields");
            GUILayout.BeginVertical();
            foreach (var i in sr.missingFields)
                GUILayout.Label($" - {i.DeclaringType.Name}.{i.Name}");
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.Label("Reflected Properties");
            GUILayout.BeginVertical();
            foreach (var i in sr.missingProperties)
                GUILayout.Label($" - {i.DeclaringType.Name}.{i.Name}");
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.Label("Baked Methods");
            GUILayout.BeginVertical();
            foreach (var kv in sr.methodIndex)
                if (kv.Value == null)
                    GUILayout.Label($" - {kv.Key.DeclaringType.Name}.{kv.Key.Name} NULL");
                else
                    GUILayout.Label($" - {kv.Value}");
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.Label("Baked Fields");
            GUILayout.BeginVertical();
            foreach (var i in sr.fieldIndex.Values)
                GUILayout.Label($" - {i}");
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.Label("Baked Properties");
            GUILayout.BeginVertical();
            foreach (var i in sr.propertyIndex.Values)
                GUILayout.Label($" - {i}");
            GUILayout.EndVertical();
            GUILayout.Space(10);


        }
    }

}