using UnityEditor;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Scripting;

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
            assemblyBuilder = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave, "./Assets/");
            var classCtorInfo = typeof(PreserveAttribute).GetConstructor(Type.EmptyTypes);
            var caBuilder = new CustomAttributeBuilder(classCtorInfo, new object[] { });
            assemblyBuilder.SetCustomAttribute(caBuilder);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyFileName, false);

        }

        internal static void Save()
        {
            assemblyBuilder.Save(assemblyFileName);
        }


        public static Type CreateProperty(PropertyInfo pi)
        {

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
            var componentField = typeBuilder.DefineField("_component", pi.DeclaringType, FieldAttributes.Public);

            CreateRegisterMethod(pi, typeBuilder, componentField);
            CreateSetComponentMethod(pi, typeBuilder, componentField);
            CreateSetMethod(pi, typeBuilder, componentField, itype);
            CreateGetMethod(pi, typeBuilder, componentField, itype);

            return typeBuilder.CreateType();
        }

        static void CreateRegisterMethod(PropertyInfo pi, TypeBuilder typeBuilder, FieldBuilder componentField)
        {
            var mb = typeBuilder.DefineMethod("AddToRegister", MethodAttributes.Static, typeof(void), null);
            var ctorParams = new Type[] { typeof(RuntimeInitializeLoadType) };
            var classCtorInfo = typeof(RuntimeInitializeOnLoadMethodAttribute).GetConstructor(ctorParams);
            var cab = new CustomAttributeBuilder(classCtorInfo, new object[] { RuntimeInitializeLoadType.BeforeSceneLoad });
            mb.SetCustomAttribute(cab);
            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, pi.DeclaringType);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Ldstr, pi.Name);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetProperty", new Type[] { typeof(string) }));
            il.Emit(OpCodes.Ldtoken, typeBuilder);
            il.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Call, typeof(SurrogateRegister).GetMethod("SetSurrogate"));
            il.Emit(OpCodes.Ret);
        }

        static void CreateSetComponentMethod(PropertyInfo pi, TypeBuilder typeBuilder, FieldBuilder componentField)
        {
            var mb = typeBuilder.DefineMethod("SetComponent", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(void), new Type[] { typeof(Component) });
            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, pi.DeclaringType);
            il.Emit(OpCodes.Stfld, componentField);
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(mb, typeof(ISurrogateProperty).GetMethod("SetComponent"));
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
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, componentField);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, setMethod);
                il.Emit(OpCodes.Ret);
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
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, componentField);
                il.Emit(OpCodes.Callvirt, getMethod);
                il.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(mb, itype.GetMethod("Get"));
        }



    }
}
