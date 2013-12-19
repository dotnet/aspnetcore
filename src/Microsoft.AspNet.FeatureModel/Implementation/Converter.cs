using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.FeatureModel.Implementation
{
    public abstract class NonGenericProxyBase
    {
        public readonly Type WrappedType;
        protected NonGenericProxyBase(Type wrappedType)
        {
            this.WrappedType = wrappedType;
        }
        public abstract object UnderlyingInstanceAsObject
        {
            get;
        }
    }

    public class BaseType<T> : NonGenericProxyBase where T : class
    {
        protected T instance;
        public BaseType(T inst)
            : base(typeof(T))
        {
            if (inst == null) throw new InvalidOperationException("should never construct proxy over null");
            this.instance = inst;
        }
        public T UnderlyingInstance
        {
            get
            {
                return instance;
            }
        }
        public override object UnderlyingInstanceAsObject
        {
            get
            {
                return instance;
            }
        }
    }

    public class Converter
    {
        public static object Convert(Type outputType, Type inputType, object input)
        {
            if (inputType == outputType) return input;

            if (!inputType.IsInterface || !outputType.IsInterface) throw new InvalidOperationException("Both types must be interfaces");

            if (inputType.GetInterfaces().Contains(outputType)) return input;

            if (input == null) return null;

            Type t = EnsureConverter(outputType, inputType);

            return Activator.CreateInstance(t, input);
        }   

        public static TOut Convert<TOut>(Type inputType, object input)
            where TOut : class
        {
            return (TOut)Convert(typeof (TOut), inputType, input);
        }

        public static TOut Convert<TIn, TOut>(TIn input)
            where TIn : class
            where TOut : class
        {
            return Convert<TOut>(typeof(TIn), input);
        }

        public static TOut Convert<TOut>(object input)
            where TOut : class
        {
            if (input == null) return null;
            var interfaceName = typeof(TOut).FullName;
            foreach (var inputType in input.GetType().GetInterfaces())
            {
                if (inputType.FullName == interfaceName)
                {
                    return Convert<TOut>(inputType, input);
                }
            }
            return null;
        }


        public static Type EnsureConverter(Type tout, Type tin)
        {
            CacheResult result;
            if (!ConverterTypeCache.TryGetValue(new Tuple<Type, Type>(tin, tout), out result))
            {
                EnsureCastPossible(tout, tin);
                return EnsureConverter(tout, tin);
            }
            else
            {
                if (result is ErrorResult)
                {
                    throw new InvalidCastException((result as ErrorResult).error);
                }
                else if (result is CurrentlyVerifyingResult)
                {
                    throw new InvalidOperationException("Type cannot be obtained in verification phase");
                }
                else if (result is TypeBuilderResult)
                {
                    return (result as TypeBuilderResult).result;
                }
                else if (result is VerificationSucceededResult)
                {
                    return CreateWrapperType(tout, tin, result as VerificationSucceededResult);
                }
                else
                {
                    throw new InvalidOperationException("Invalid cache state");
                }
            }
        }

        class CacheResult
        {
        }

        class TypeBuilderResult : CacheResult
        {
            internal TypeBuilderResult(Type result)
            {
                this.result = result;
            }
            internal readonly Type result;
        }
        class ErrorResult : CacheResult
        {
            internal ErrorResult(string error)
            {
                this.error = error;
            }
            internal readonly string error;
        }
        class CurrentlyVerifyingResult : CacheResult
        {
        }
        enum SuccessKind
        {
            Identity,
            SubInterface,
            Wrapper,
        }
        class VerificationSucceededResult : CacheResult
        {
            internal VerificationSucceededResult(SuccessKind kind)
            {
                this.kind = kind;
            }
            internal VerificationSucceededResult(Dictionary<MethodInfo, MethodInfo> mappings)
            {
                this.kind = SuccessKind.Wrapper;
                this.methodMappings = mappings;
            }
            internal readonly SuccessKind kind;
            internal readonly Dictionary<MethodInfo, MethodInfo> methodMappings;
        }

        static Dictionary<Tuple<Type, Type>, CacheResult> ConverterTypeCache = new Dictionary<Tuple<Type, Type>, CacheResult>();
        static ConditionalWeakTable<object, NonGenericProxyBase> ConverterInstanceCache = new ConditionalWeakTable<object, NonGenericProxyBase>();
        static AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ProxyHolderAssembly"), AssemblyBuilderAccess.Run);
        static ModuleBuilder modb = ab.DefineDynamicModule("Main Module");

        class EqComparer : IEqualityComparer<ParameterInfo>
        {

            bool IEqualityComparer<ParameterInfo>.Equals(ParameterInfo x, ParameterInfo y)
            {
                return EqualTypes(x.ParameterType, y.ParameterType);
            }

            int IEqualityComparer<ParameterInfo>.GetHashCode(ParameterInfo obj)
            {
                return obj.GetHashCode();
            }
        }

        static bool EqualTypes(Type sourceType, Type targetType)
        {
            return EnsureCastPossible(targetType, sourceType);
        }



        static MethodInfo FindCorrespondingMethod(Type targetType, Type sourceType, MethodInfo miTarget)
        {
            MethodInfo[] sms = sourceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where((MethodInfo mi) => mi.Name == miTarget.Name).ToArray();
            if (sms != null && sms.Length != 0)
            {
                MethodInfo[] sm = null;
                try
                {
                    sm = sms.Where((mi) => (mi.GetParameters().SequenceEqual(miTarget.GetParameters(), new EqComparer()))).ToArray();
                }
                catch
                {
                }
                if (sm != null && sm.Length != 0)
                {
                    if (sm.Length > 1) return null;
                    if (EqualTypes(sm[0].ReturnType, miTarget.ReturnType))
                    {
                        return sm[0];
                    }
                }
            }
            MethodInfo[] rval = sourceType.GetInterfaces().Select((inheritedItf) => FindCorrespondingMethod(targetType, inheritedItf, miTarget)).ToArray();
            if (rval == null || rval.Length == 0) return null;
            if (rval.Length > 1) return null;
            return rval[0];

        }


        static void AddMethod(Type targetType, Type sourceType, TypeBuilder tb, MethodInfo miTarget, MethodInfo miSource)
        {
            ParameterInfo[] pisTarget = miTarget.GetParameters();
            ParameterInfo[] pisSource = miSource.GetParameters();
            MethodBuilder metb;
            Type[] typesTarget;
            if (pisTarget == null || pisTarget.Length == 0)
            {
                metb = tb.DefineMethod(miTarget.Name, MethodAttributes.Virtual, CallingConventions.HasThis, miTarget.ReturnType, null);
                pisTarget = new ParameterInfo[0];
                typesTarget = new Type[0];
            }
            else
            {
                typesTarget = pisTarget.Select((pi) => pi.ParameterType).ToArray();
                Type[][] requiredCustomMods = pisTarget.Select((pi) => pi.GetRequiredCustomModifiers()).ToArray();
                Type[][] optionalCustomMods = pisTarget.Select((pi) => pi.GetOptionalCustomModifiers()).ToArray();

                metb = tb.DefineMethod(miTarget.Name, MethodAttributes.Virtual, CallingConventions.HasThis, miTarget.ReturnType, null, null, typesTarget, requiredCustomMods, optionalCustomMods);
            }

            ILGenerator il = metb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, tb.BaseType.GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance));
            for (int pi = 0; pi < pisTarget.Length; pi++)
            {
                il.Emit(OpCodes.Ldarg, pi + 1);
                EmitParamConversion(il, typesTarget[pi], pisSource[pi].ParameterType);
            }
            il.EmitCall(OpCodes.Callvirt, miSource, null);

            EmitParamConversion(il, miSource.ReturnType, miTarget.ReturnType);
            il.Emit(OpCodes.Ret);

            tb.DefineMethodOverride(metb, miTarget);
        }

        static void EmitParamConversion(ILGenerator il, Type typeOnStack, Type typeRequiredInSignature)
        {
            if (typeOnStack != typeRequiredInSignature)
            {
                if (typeOnStack.GetInterfaces().Contains(typeRequiredInSignature))
                {
                    il.Emit(OpCodes.Castclass, typeRequiredInSignature);
                }
                else
                {
                    Label lEnd = il.DefineLabel();
                    Label lCreateProxy = il.DefineLabel();
                    il.Emit(OpCodes.Dup);   //   o o
                    il.Emit(OpCodes.Brfalse_S, lEnd); // o
                    il.Emit(OpCodes.Dup); // o o
                    il.Emit(OpCodes.Isinst, typeof(NonGenericProxyBase));  // o [p/n]
                    il.Emit(OpCodes.Brfalse_S, lCreateProxy);  // o
                    il.Emit(OpCodes.Isinst, typeof(NonGenericProxyBase));  // p
                    il.EmitCall(OpCodes.Callvirt, typeof(NonGenericProxyBase).GetMethod("get_UnderlyingInstanceAsObject"), null);  // uo
                    il.Emit(OpCodes.Dup); // uo uo
                    il.Emit(OpCodes.Isinst, typeRequiredInSignature);  // uo [ro/n]
                    il.Emit(OpCodes.Brtrue_S, lEnd);  // uo
                    il.MarkLabel(lCreateProxy); // uo
                    Type paramProxyType = EnsureConverter(typeRequiredInSignature, typeOnStack);
                    il.Emit(OpCodes.Newobj, paramProxyType.GetConstructors()[0]);
                    il.MarkLabel(lEnd); // ro
                }
            }
        }



        static bool EnsureCastPossible(Type targetType, Type sourceType)
        {
            var key = new Tuple<Type, Type>(sourceType, targetType);
            CacheResult cr = null;
            if (ConverterTypeCache.TryGetValue(key, out cr))
            {
                if (cr is CurrentlyVerifyingResult || cr is VerificationSucceededResult || cr is TypeBuilderResult) return true;
                if (cr is ErrorResult) return false;
            }
            if (targetType == sourceType)
            {
                ConverterTypeCache[key] = new VerificationSucceededResult(SuccessKind.Identity);
                return true;
            }
            if (targetType.GetInterfaces().Contains(sourceType))
            {
                ConverterTypeCache[key] = new VerificationSucceededResult(SuccessKind.SubInterface);
                return true;
            }
            if (!targetType.IsInterface || !sourceType.IsInterface)
            {
                ConverterTypeCache[key] = new ErrorResult("Cannot cast " + sourceType + " to " + targetType);
                return false;
            }
            bool success = false;
            ConverterTypeCache[key] = new CurrentlyVerifyingResult();
            try
            {
                Dictionary<MethodInfo, MethodInfo> mappings = new Dictionary<MethodInfo, MethodInfo>();
                foreach (MethodInfo mi in targetType.GetMethods().Concat(targetType.GetInterfaces().SelectMany((itf) => itf.GetMethods())))
                {
                    MethodInfo mapping = FindCorrespondingMethod(targetType, sourceType, mi);
                    if (mapping == null)
                    {
                        ConverterTypeCache[key] = new ErrorResult("Can not cast " + sourceType + " to " + targetType + " because of missing method: " + mi.Name);
                        return false;
                    }
                    mappings[mi] = mapping;
                }
                ConverterTypeCache[key] = new VerificationSucceededResult(mappings);
                success = true;
                return true;
            }
            finally
            {
                if (!success)
                {
                    if (!(ConverterTypeCache[key] is ErrorResult))
                    {
                        ConverterTypeCache[key] = new ErrorResult("Can not cast " + sourceType + " to " + targetType);
                    }
                }
            }
        }

        static int counter = 0;
        static Type CreateWrapperType(Type targetType, Type sourceType, VerificationSucceededResult result)
        {
            Dictionary<MethodInfo, MethodInfo> mappings = result.methodMappings;
            Type baseType = Assembly.GetExecutingAssembly().GetType("InterfaceMapper.BaseType`1").MakeGenericType(sourceType);
            TypeBuilder tb = modb.DefineType("ProxyType" + counter++ + " wrapping:" + sourceType.Name + " to look like:" + targetType.Name, TypeAttributes.Class, baseType, new Type[] { targetType });
            ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { sourceType });
            ILGenerator il = cb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, sourceType);
            il.Emit(OpCodes.Call, baseType.GetConstructor(new Type[] { sourceType }));
            il.Emit(OpCodes.Ret);
            var tuple = new Tuple<Type, Type>(sourceType, targetType);
            try
            {
                ConverterTypeCache[tuple] = new TypeBuilderResult(tb);
                foreach (MethodInfo mi in targetType.GetMethods().Concat(targetType.GetInterfaces().SelectMany((itf) => itf.GetMethods())))
                {
                    AddMethod(targetType, sourceType, tb, mi, mappings[mi]);
                }
                Type t = tb.CreateType();
                ConverterTypeCache[tuple] = new TypeBuilderResult(t);
                return t;
            }
            catch (Exception e)
            {
                ConverterTypeCache[tuple] = new ErrorResult(e.Message);
                throw;
            }
        }

    }
}
