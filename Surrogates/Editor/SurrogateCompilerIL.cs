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
    public static class SurrogateCompiler
    {
        static string assemblyName = "Xyzzy.Surrogate";
        static string assemblyFileName;
        static ModuleBuilder moduleBuilder;
        static AssemblyBuilder assemblyBuilder;

        static SurrogateCompiler()
        {
            var domain = Thread.GetDomain();
            var asmName = new AssemblyName();
            asmName.Name = assemblyName;
            assemblyFileName = assemblyName + ".dll";
            var assetPath = AssetDatabase.GetAssetPath(SurrogateRegister.Instance);
            Debug.Log(assetPath);
            var path = System.IO.Path.GetDirectoryName(assetPath);
            assemblyBuilder = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave, path);
            var classCtorInfo = typeof(PreserveAttribute).GetConstructor(Type.EmptyTypes);
            var caBuilder = new CustomAttributeBuilder(classCtorInfo, new object[] { });
            assemblyBuilder.SetCustomAttribute(caBuilder);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyFileName, false);
            propertyIndex = new Dictionary<PropertyInfo, TypeBuilder>();
            fieldIndex = new Dictionary<FieldInfo, TypeBuilder>();
            methodIndex = new Dictionary<MethodInfo, TypeBuilder>();
        }

        static Dictionary<PropertyInfo, TypeBuilder> propertyIndex;
        static Dictionary<MethodInfo, TypeBuilder> methodIndex;
        static Dictionary<FieldInfo, TypeBuilder> fieldIndex;

        public static void Save()
        {
            CreateMethodIndex();
            assemblyBuilder.Save(assemblyFileName);
            propertyIndex.Clear();
            fieldIndex.Clear();
            methodIndex.Clear();
        }

        static void CreateMethodIndex()
        {
            var className = $"SurrogateMethodIndex";

            var typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public);
            var mb = typeBuilder.DefineMethod("Init", MethodAttributes.Static | MethodAttributes.Public, typeof(void), Type.EmptyTypes);
            var il = mb.GetILGenerator();

            il.Emit(OpCodes.Nop);
            foreach (var kv in methodIndex)
            {
                var mi = kv.Key;
                var parameters = mi.GetParameters();

                il.Emit(OpCodes.Ldtoken, mi.DeclaringType);
                il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
                il.Emit(OpCodes.Ldstr, mi.Name);

                EmitInt(il, parameters.Length);
                il.Emit(OpCodes.Newarr, typeof(System.Type));
                for (var i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    EmitInt(il, i);
                    il.Emit(OpCodes.Ldtoken, parameters[i].ParameterType);
                    il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
                    il.Emit(OpCodes.Stelem_Ref);
                }
                il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(Type[]) }));
                il.Emit(OpCodes.Ldtoken, kv.Value);
                il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
                il.Emit(OpCodes.Call, typeof(SurrogateRegister).GetMethod("SetSurrogate", new[] { typeof(MethodInfo), typeof(Type) }));
                il.Emit(OpCodes.Nop);
            }
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);

            typeBuilder.CreateType();
        }

        static void EmitInt(ILGenerator il, int v)
        {
            switch (v)
            {
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (v < 255)
                        il.Emit(OpCodes.Ldc_I4_S, (short)v);
                    else
                        il.Emit(OpCodes.Ldc_I4, v);
                    break;
            }
        }



        public static Type CreateField(FieldInfo fi)
        {
            try
            {
                fi = fi.DeclaringType.GetField(fi.Name);
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }

            var className = $"F_{(uint)fi.DeclaringType.GetHashCode()}_{(uint)fi.GetHashCode()}";

            // Debug.Log("Creating Class: " + className);
            TypeBuilder typeBuilder;
            try
            {
                typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public);
            }
            catch (ArgumentException)
            {
                Debug.Log("Duplicate:" + className);
                return null;
            }
            var itype = typeof(ISurrogateProperty<>).MakeGenericType(fi.FieldType);
            typeBuilder.AddInterfaceImplementation(itype);

            // CreateFieldRegisterMethod(fi.DeclaringType, fi.Name, typeBuilder);
            if (fi.IsStatic)
            {
                CreateSetComponentMethod(fi.DeclaringType, typeBuilder, null);
                CreateSetMethod(fi, typeBuilder, null, itype);
                CreateGetMethod(fi, typeBuilder, null, itype);
            }
            else
            {
                var componentField = typeBuilder.DefineField("_component", fi.DeclaringType, FieldAttributes.Public);
                CreateSetComponentMethod(fi.DeclaringType, typeBuilder, componentField);
                CreateSetMethod(fi, typeBuilder, componentField, itype);
                CreateGetMethod(fi, typeBuilder, componentField, itype);
            }
            fieldIndex[fi] = typeBuilder;
            return typeBuilder.CreateType();
        }


        public static Type CreateProperty(PropertyInfo pi)
        {
            try
            {
                pi = pi.DeclaringType.GetProperty(pi.Name);
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }

            var className = $"P_{(uint)pi.DeclaringType.GetHashCode()}_{(uint)pi.GetHashCode()}";

            // Debug.Log("Creating Class: " + className);
            TypeBuilder typeBuilder;
            try
            {
                typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public);
            }
            catch (ArgumentException e)
            {
                Debug.Log("Duplicate:" + className);
                return null;
            }
            var itype = typeof(ISurrogateProperty<>).MakeGenericType(pi.PropertyType);
            typeBuilder.AddInterfaceImplementation(itype);

            // CreatePropertyRegisterMethod(pi.DeclaringType, pi.Name, typeBuilder);
            if (pi.GetGetMethod().IsStatic)
            {
                CreateSetComponentMethod(pi.DeclaringType, typeBuilder, null);
                CreateSetMethod(pi, typeBuilder, null, itype);
                CreateGetMethod(pi, typeBuilder, null, itype);
            }
            else
            {
                var componentField = typeBuilder.DefineField("_component", pi.DeclaringType, FieldAttributes.Public);
                CreateSetComponentMethod(pi.DeclaringType, typeBuilder, componentField);
                CreateSetMethod(pi, typeBuilder, componentField, itype);
                CreateGetMethod(pi, typeBuilder, componentField, itype);
            }
            propertyIndex[pi] = typeBuilder;
            return typeBuilder.CreateType();
        }

        static void CreatePropertyRegisterMethod(Type declaringType, string name, TypeBuilder typeBuilder)
        {
            var mb = typeBuilder.DefineMethod("AddToRegister", MethodAttributes.Static, typeof(void), null);
            var ctorParams = new Type[] { typeof(RuntimeInitializeLoadType) };
            var classCtorInfo = typeof(RuntimeInitializeOnLoadMethodAttribute).GetConstructor(ctorParams);
            var cab = new CustomAttributeBuilder(classCtorInfo, new object[] { RuntimeInitializeLoadType.AfterAssembliesLoaded });
            mb.SetCustomAttribute(cab);
            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, declaringType);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetProperty", new Type[] { typeof(string) }));
            il.Emit(OpCodes.Ldtoken, typeBuilder);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Call, typeof(SurrogateRegister).GetMethod("SetSurrogate", new[] { typeof(PropertyInfo), typeof(Type) }));
            il.Emit(OpCodes.Ret);
        }

        static void CreateFieldRegisterMethod(Type declaringType, string name, TypeBuilder typeBuilder)
        {
            var mb = typeBuilder.DefineMethod("AddToRegister", MethodAttributes.Static, typeof(void), null);
            var ctorParams = new Type[] { typeof(RuntimeInitializeLoadType) };
            var classCtorInfo = typeof(RuntimeInitializeOnLoadMethodAttribute).GetConstructor(ctorParams);
            var cab = new CustomAttributeBuilder(classCtorInfo, new object[] { RuntimeInitializeLoadType.AfterAssembliesLoaded });
            mb.SetCustomAttribute(cab);
            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, declaringType);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetField", new Type[] { typeof(string) }));
            il.Emit(OpCodes.Ldtoken, typeBuilder);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Call, typeof(SurrogateRegister).GetMethod("SetSurrogate", new[] { typeof(FieldInfo), typeof(Type) }));
            il.Emit(OpCodes.Ret);
        }


        static void CreateSetComponentMethod(Type declaringType, TypeBuilder typeBuilder, FieldBuilder componentField)
        {
            var mb = typeBuilder.DefineMethod("SetComponent", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(void), new Type[] { typeof(Component) });
            var il = mb.GetILGenerator();
            if (componentField == null)
            {
                il.Emit(OpCodes.Newobj, typeof(System.NotImplementedException));
                il.Emit(OpCodes.Throw);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, declaringType);
                il.Emit(OpCodes.Stfld, componentField);
                il.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(mb, typeof(ISurrogate).GetMethod("SetComponent"));
        }

        static void CreateSetMethod(PropertyInfo pi, TypeBuilder typeBuilder, FieldBuilder componentField, Type itype)
        {
            var mb = typeBuilder.DefineMethod("Set", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(void), new Type[] { pi.PropertyType });
            var setMethod = pi.GetSetMethod();
            var il = mb.GetILGenerator();
            if (setMethod == null)
            {
                il.Emit(OpCodes.Newobj, typeof(System.NotImplementedException));
                il.Emit(OpCodes.Throw);
            }
            else
            {
                if (setMethod.IsStatic)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, setMethod);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, componentField);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Callvirt, setMethod);
                    il.Emit(OpCodes.Ret);
                }
            }
            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Set"));
        }

        static void CreateGetMethod(PropertyInfo pi, TypeBuilder typeBuilder, FieldBuilder componentField, Type itype)
        {
            var mb = typeBuilder.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, pi.PropertyType, null);
            var il = mb.GetILGenerator();
            var getMethod = pi.GetGetMethod();
            if (getMethod == null)
            {
                il.Emit(OpCodes.Newobj, typeof(System.NotImplementedException));
                il.Emit(OpCodes.Throw);
            }
            else
            {
                if (getMethod.IsStatic)
                {
                    il.Emit(OpCodes.Call, getMethod);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, componentField);
                    il.Emit(OpCodes.Callvirt, getMethod);
                    il.Emit(OpCodes.Ret);
                }
            }
            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Get"));
        }

        public static Type CreateAction(MethodInfo mi)
        {
            var className = $"M_{(uint)mi.DeclaringType.GetHashCode()}_{(uint)mi.GetHashCode()}";

            TypeBuilder typeBuilder;
            try
            {
                typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public);
            }
            catch (ArgumentException)
            {
                Debug.Log("Duplicate:" + className);
                return null;
            }
            AddEditorBrowsableAttribute(typeBuilder);
            Type itype;
            if (mi.ReturnType == typeof(void))
                itype = typeof(ISurrogateAction);
            else
                itype = typeof(ISurrogateAction<>).MakeGenericType(mi.ReturnType);

            typeBuilder.AddInterfaceImplementation(itype);

            // CreateActionRegisterMethod(mi, typeBuilder);

            if (mi.IsStatic)
            {
                CreateSetComponentMethod(mi.DeclaringType, typeBuilder, null);
                CreateStaticInvokeMethod(mi, typeBuilder, itype);
            }
            else
            {
                // Debug.Log("Creating Class: " + className);
                var componentField = typeBuilder.DefineField("_component", mi.DeclaringType, FieldAttributes.Public);
                CreateSetComponentMethod(mi.DeclaringType, typeBuilder, componentField);
                CreateInvokeMethod(mi, typeBuilder, componentField, itype);
            }
            methodIndex[mi] = typeBuilder;
            return typeBuilder.CreateType();
        }


        static void AddEditorBrowsableAttribute(TypeBuilder typeBuilder)
        {
            var classCtorInfo = typeof(System.ComponentModel.EditorBrowsableAttribute).GetConstructor(new[] { typeof(System.ComponentModel.EditorBrowsableState) });
            var caBuilder = new CustomAttributeBuilder(classCtorInfo, new object[] { System.ComponentModel.EditorBrowsableState.Never });
            typeBuilder.SetCustomAttribute(caBuilder);
        }

        static void CreateInvokeMethod(MethodInfo mi, TypeBuilder typeBuilder, FieldBuilder componentField, Type itype)
        {
            var mb = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, mi.ReturnType, Type.EmptyTypes);
            var il = mb.GetILGenerator();
            if (mi == null)
            {
                il.Emit(OpCodes.Newobj, typeof(System.NotImplementedException));
                il.Emit(OpCodes.Throw);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, componentField);
                foreach (var p in mi.GetParameters())
                {
                    var field = typeBuilder.DefineField(p.Name, p.ParameterType, FieldAttributes.Public);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);
                }
                il.Emit(OpCodes.Call, mi);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Invoke"));
        }

        static void CreateStaticInvokeMethod(MethodInfo mi, TypeBuilder typeBuilder, Type itype)
        {
            var mb = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, mi.ReturnType, Type.EmptyTypes);
            var il = mb.GetILGenerator();
            if (mi == null)
            {
                il.Emit(OpCodes.Newobj, typeof(System.NotImplementedException));
                il.Emit(OpCodes.Throw);
            }
            else
            {
                il.Emit(OpCodes.Nop);
                foreach (var p in mi.GetParameters())
                {
                    var field = typeBuilder.DefineField(p.Name, p.ParameterType, FieldAttributes.Public);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);
                }
                il.Emit(OpCodes.Call, mi);
                il.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Invoke"));
        }

        static void CreateActionRegisterMethod(MethodInfo mi, TypeBuilder typeBuilder)
        {
            var parameters = mi.GetParameters();
            var mb = typeBuilder.DefineMethod("AddToRegister", MethodAttributes.Static, typeof(void), null);
            var ctorParams = new Type[] { typeof(RuntimeInitializeLoadType) };
            var classCtorInfo = typeof(RuntimeInitializeOnLoadMethodAttribute).GetConstructor(ctorParams);
            var cab = new CustomAttributeBuilder(classCtorInfo, new object[] { RuntimeInitializeLoadType.BeforeSplashScreen });
            mb.SetCustomAttribute(cab);
            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, mi.DeclaringType);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Ldstr, mi.Name);
            //TODO: populate array with mi.Parameters
            il.Emit(OpCodes.Ldc_I4_S, parameters.Length);
            il.Emit(OpCodes.Newarr, typeof(System.Type));

            for (var i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4_S, i);
                il.Emit(OpCodes.Ldtoken, parameters[i].ParameterType);
                il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
                il.Emit(OpCodes.Stelem_Ref);
            }
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(Type[]) }));
            il.Emit(OpCodes.Ldtoken, typeBuilder);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Call, typeof(SurrogateRegister).GetMethod("SetSurrogate", new[] { typeof(MethodInfo), typeof(Type) }));
            il.Emit(OpCodes.Ret);
        }

        static void CreateSetMethod(FieldInfo fi, TypeBuilder typeBuilder, FieldBuilder componentField, Type itype)
        {
            var mb = typeBuilder.DefineMethod("Set", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(void), new Type[] { fi.FieldType });

            var il = mb.GetILGenerator();
            if (fi.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stsfld, fi);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, componentField);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, fi);
                il.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Set"));
        }

        static void CreateGetMethod(FieldInfo fi, TypeBuilder typeBuilder, FieldBuilder componentField, Type itype)
        {
            var mb = typeBuilder.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, fi.FieldType, null);
            var il = mb.GetILGenerator();

            if (fi.IsStatic)
            {
                il.Emit(OpCodes.Ldsfld, fi);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, componentField);
                il.Emit(OpCodes.Ldfld, fi);
                il.Emit(OpCodes.Ret);
            }

            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Get"));
        }
    }
}
