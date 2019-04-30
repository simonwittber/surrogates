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
            var path = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(SurrogateRegister.Instance));
            assemblyBuilder = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave, path);
            var classCtorInfo = typeof(PreserveAttribute).GetConstructor(Type.EmptyTypes);
            var caBuilder = new CustomAttributeBuilder(classCtorInfo, new object[] { });
            assemblyBuilder.SetCustomAttribute(caBuilder);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyFileName, false);
        }

        public static void Save()
        {
            assemblyBuilder.Save(assemblyFileName);
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

            var className = "_" + pi.DeclaringType.Name + "_" + pi.Name;

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
            var itype = typeof(ISurrogateProperty<>).MakeGenericType(pi.PropertyType);
            typeBuilder.AddInterfaceImplementation(itype);



            CreatePropertyRegisterMethod(pi.DeclaringType, pi.Name, typeBuilder);
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
            return typeBuilder.CreateType();
        }

        static void CreatePropertyRegisterMethod(Type declaringType, string name, TypeBuilder typeBuilder)
        {
            var mb = typeBuilder.DefineMethod("AddToRegister", MethodAttributes.Static, typeof(void), null);
            var ctorParams = new Type[] { typeof(RuntimeInitializeLoadType) };
            var classCtorInfo = typeof(RuntimeInitializeOnLoadMethodAttribute).GetConstructor(ctorParams);
            var cab = new CustomAttributeBuilder(classCtorInfo, new object[] { RuntimeInitializeLoadType.BeforeSceneLoad });
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
            mi = mi.DeclaringType.GetMethod(mi.Name, new Type[] { });
            var className = "_" + mi.DeclaringType.Name + "_" + mi.Name;
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
            var itype = typeof(ISurrogateAction);
            typeBuilder.AddInterfaceImplementation(itype);
            mi.DeclaringType.GetMethod(mi.Name, new Type[] { });
            CreateActionRegisterMethod(mi.DeclaringType, mi.Name, typeBuilder);

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
            return typeBuilder.CreateType();
        }

        static void CreateInvokeMethod(MethodInfo mi, TypeBuilder typeBuilder, FieldBuilder componentField, Type itype)
        {
            var mb = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(void), null);
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
                il.Emit(OpCodes.Callvirt, mi);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Invoke"));
        }

        static void CreateStaticInvokeMethod(MethodInfo mi, TypeBuilder typeBuilder, Type itype)
        {
            var mb = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(void), null);
            var il = mb.GetILGenerator();
            if (mi == null)
            {
                il.Emit(OpCodes.Newobj, typeof(System.NotImplementedException));
                il.Emit(OpCodes.Throw);
            }
            else
            {
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Call, mi);
                il.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Invoke"));
        }

        static void CreateActionRegisterMethod(Type declaringType, string name, TypeBuilder typeBuilder)
        {
            var mb = typeBuilder.DefineMethod("AddToRegister", MethodAttributes.Static, typeof(void), null);
            var ctorParams = new Type[] { typeof(RuntimeInitializeLoadType) };
            var classCtorInfo = typeof(RuntimeInitializeOnLoadMethodAttribute).GetConstructor(ctorParams);
            var cab = new CustomAttributeBuilder(classCtorInfo, new object[] { RuntimeInitializeLoadType.BeforeSceneLoad });
            mb.SetCustomAttribute(cab);
            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, declaringType);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Newarr, typeof(System.Type));
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(Type[]) }));
            il.Emit(OpCodes.Ldtoken, typeBuilder);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Call, typeof(SurrogateRegister).GetMethod("SetSurrogate", new[] { typeof(MethodInfo), typeof(Type) }));
            il.Emit(OpCodes.Ret);
        }
    }
}
