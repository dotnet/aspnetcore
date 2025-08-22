// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.AspNetCore.SignalR.Internal;

[RequiresDynamicCode("Creating a proxy instance requires generating code at runtime")]
internal static class TypedClientBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
{
    private const string ClientModuleName = "Microsoft.AspNetCore.SignalR.TypedClientBuilder";

    // There is one static instance of _builder per T
    private static readonly Lazy<Func<IClientProxy, T>> _builder = new Lazy<Func<IClientProxy, T>>(GenerateClientBuilder);

    private static readonly PropertyInfo CancellationTokenNoneProperty = typeof(CancellationToken).GetProperty("None", BindingFlags.Public | BindingFlags.Static)!;

    private static readonly ConstructorInfo ObjectConstructor = typeof(object).GetConstructors().Single();

    private static readonly Type[] ParameterTypes = new Type[] { typeof(IClientProxy) };

    public static T Build(IClientProxy proxy)
    {
        return _builder.Value(proxy);
    }

    public static void Validate()
    {
        // The following will throw if T is not a valid type
        _ = _builder.Value;
    }

    private static Func<IClientProxy, T> GenerateClientBuilder()
    {
        VerifyInterface(typeof(T));

        var assemblyName = new AssemblyName(ClientModuleName);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(ClientModuleName);
        var clientType = GenerateInterfaceImplementation(moduleBuilder);

        var factoryMethod = clientType.GetMethod(nameof(Build), BindingFlags.Public | BindingFlags.Static);
        return (Func<IClientProxy, T>)factoryMethod!.CreateDelegate(typeof(Func<IClientProxy, T>));
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static Type GenerateInterfaceImplementation(ModuleBuilder moduleBuilder)
    {
        var name = ClientModuleName + "." + typeof(T).Name + "Impl";

        var type = moduleBuilder.DefineType(name, TypeAttributes.Public, typeof(object), new[] { typeof(T) });

        var proxyField = type.DefineField("_proxy", typeof(IClientProxy), FieldAttributes.Private | FieldAttributes.InitOnly);

        var ctor = BuildConstructor(type, proxyField);

        // Because a constructor doesn't return anything, it can't be wrapped in a
        // delegate directly, so we emit a factory method that just takes the IClientProxy,
        // invokes the constructor (using newobj) and returns the new instance of type T.
        BuildFactoryMethod(type, ctor);

        foreach (var method in GetAllInterfaceMethods(typeof(T)))
        {
            BuildMethod(type, method, proxyField);
        }

        return type.CreateType();
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2062:UnrecognizedReflectionPattern",
        Justification = "interfaceType is annotated as preserve All members, so any Types returned from GetInterfaces will be preserved as well. https://github.com/mono/linker/issues/1731 tracks not emitting warnings here.")]
    private static IEnumerable<MethodInfo> GetAllInterfaceMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
    {
        foreach (var parent in interfaceType.GetInterfaces())
        {
            // interfaceType is annotated as preserve All members, so any Types returned from GetInterfaces will be preserved as well. https://github.com/mono/linker/issues/1731 tracks not emitting warnings here.
#pragma warning disable IL2072
            foreach (var parentMethod in GetAllInterfaceMethods(parent))
#pragma warning restore IL2072
            {
                yield return parentMethod;
            }
        }

        foreach (var method in interfaceType.GetMethods())
        {
            yield return method;
        }
    }

    private static ConstructorInfo BuildConstructor(TypeBuilder type, FieldInfo proxyField)
    {
        var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, ParameterTypes);

        var generator = ctor.GetILGenerator();

        // Call object constructor
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Call, ObjectConstructor);

        // Assign constructor argument to the proxyField
        generator.Emit(OpCodes.Ldarg_0); // type
        generator.Emit(OpCodes.Ldarg_1); // type proxyfield
        generator.Emit(OpCodes.Stfld, proxyField); // type.proxyField = proxyField
        generator.Emit(OpCodes.Ret);

        return ctor;
    }

    private static void BuildMethod(TypeBuilder type, MethodInfo interfaceMethodInfo, FieldInfo proxyField)
    {
        var methodAttributes =
              MethodAttributes.Public
            | MethodAttributes.Virtual
            | MethodAttributes.Final
            | MethodAttributes.HideBySig
            | MethodAttributes.NewSlot;

        var parameters = interfaceMethodInfo.GetParameters();
        var paramTypes = parameters.Select(param => param.ParameterType).ToArray();
        var returnType = interfaceMethodInfo.ReturnType;
        bool isInvoke = returnType != typeof(Task);

        var methodBuilder = type.DefineMethod(interfaceMethodInfo.Name, methodAttributes);

        MethodInfo invokeMethod;
        if (isInvoke)
        {
            invokeMethod = typeof(ISingleClientProxy).GetMethod(
                nameof(ISingleClientProxy.InvokeCoreAsync), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] { typeof(string), typeof(object[]), typeof(CancellationToken) }, null)!
                .MakeGenericMethod(returnType.GenericTypeArguments);
        }
        else
        {
            invokeMethod = typeof(IClientProxy).GetMethod(
                nameof(IClientProxy.SendCoreAsync), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] { typeof(string), typeof(object[]), typeof(CancellationToken) }, null)!;
        }

        methodBuilder.SetReturnType(interfaceMethodInfo.ReturnType);
        methodBuilder.SetParameters(paramTypes);

        // Sets the number of generic type parameters
        var genericTypeNames =
            paramTypes.Where(p => p.IsGenericParameter).Select(p => p.Name).Distinct().ToArray();

        if (genericTypeNames.Length > 0)
        {
            methodBuilder.DefineGenericParameters(genericTypeNames);
        }

        // Check to see if the last parameter of the method is a CancellationToken
        bool hasCancellationToken = paramTypes.LastOrDefault() == typeof(CancellationToken);
        if (hasCancellationToken)
        {
            // remove CancellationToken from input paramTypes
            paramTypes = paramTypes.Take(paramTypes.Length - 1).ToArray();
        }

        var methodName =
                interfaceMethodInfo.GetCustomAttribute<HubMethodNameAttribute>()?.Name ??
                interfaceMethodInfo.Name;

        var generator = methodBuilder.GetILGenerator();

        // Declare local variable to store the arguments to IClientProxy.SendCoreAsync
        generator.DeclareLocal(typeof(object[]));

        // Get IClientProxy
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldfld, proxyField);

        var isTypeLabel = generator.DefineLabel();
        if (isInvoke)
        {
            var singleClientProxyType = typeof(ISingleClientProxy);
            /*
            if (_proxy is ISingleClientProxy singleClientProxy)
            {
                return ((ISingleClientProxy)_proxy).InvokeAsync<T>(methodName, args, cancellationToken);
            }
            throw new InvalidOperationException("InvokeAsync only works with Single clients.");
             */
            generator.Emit(OpCodes.Isinst, singleClientProxyType);
            generator.Emit(OpCodes.Brtrue_S, isTypeLabel);

            generator.Emit(OpCodes.Ldstr, "InvokeAsync only works with Single clients.");
            generator.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) })!);
            generator.Emit(OpCodes.Throw);

            generator.MarkLabel(isTypeLabel);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, proxyField);
            generator.Emit(OpCodes.Castclass, singleClientProxyType);
        }

        // The first argument to IClientProxy.SendCoreAsync is this method's name
        generator.Emit(OpCodes.Ldstr, methodName);

        // Create an new object array to hold all the parameters to this method
        generator.Emit(OpCodes.Ldc_I4, paramTypes.Length); // Stack:
        generator.Emit(OpCodes.Newarr, typeof(object)); // allocate object array
        generator.Emit(OpCodes.Stloc_0);

        // Store each parameter in the object array
        for (var i = 0; i < paramTypes.Length; i++)
        {
            generator.Emit(OpCodes.Ldloc_0); // Object array loaded
            generator.Emit(OpCodes.Ldc_I4, i);
            generator.Emit(OpCodes.Ldarg, i + 1); // i + 1
            generator.Emit(OpCodes.Box, paramTypes[i]);
            generator.Emit(OpCodes.Stelem_Ref);
        }

        // Load parameter array on to the stack.
        generator.Emit(OpCodes.Ldloc_0);

        if (hasCancellationToken)
        {
            // Get CancellationToken from input argument and put it on the stack
            generator.Emit(OpCodes.Ldarg, paramTypes.Length + 1);
        }
        else
        {
            // Get 'CancellationToken.None' and put it on the stack, for when method does not have CancellationToken
            generator.Emit(OpCodes.Call, CancellationTokenNoneProperty.GetMethod!);
        }

        // Send!
        generator.Emit(OpCodes.Callvirt, invokeMethod);

        generator.Emit(OpCodes.Ret); // Return the Task returned by 'invokeMethod'
    }

    private static void BuildFactoryMethod(TypeBuilder type, ConstructorInfo ctor)
    {
        var method = type.DefineMethod(nameof(Build), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(T), ParameterTypes);

        var generator = method.GetILGenerator();

        generator.Emit(OpCodes.Ldarg_0); // Load the IClientProxy argument onto the stack
        generator.Emit(OpCodes.Newobj, ctor); // Call the generated constructor with the proxy
        generator.Emit(OpCodes.Ret); // Return the typed client
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2062:UnrecognizedReflectionPattern",
        Justification = "interfaceType is annotated as preserve All members, so any Types returned from GetInterfaces will be preserved as well. https://github.com/mono/linker/issues/1731 tracks not emitting warnings here.")]
    private static void VerifyInterface([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
    {
        if (!interfaceType.IsInterface)
        {
            throw new InvalidOperationException("Type must be an interface.");
        }

        if (interfaceType.GetProperties().Length != 0)
        {
            throw new InvalidOperationException("Type must not contain properties.");
        }

        if (interfaceType.GetEvents().Length != 0)
        {
            throw new InvalidOperationException("Type must not contain events.");
        }

        foreach (var method in interfaceType.GetMethods())
        {
            VerifyMethod(method);
        }

        foreach (var parent in interfaceType.GetInterfaces())
        {
            // interfaceType is annotated as preserve All members, so any Types returned from GetInterfaces will be preserved as well. https://github.com/mono/linker/issues/1731 tracks not emitting warnings here.
#pragma warning disable IL2072
            VerifyInterface(parent);
#pragma warning restore IL2072
        }
    }

    private static void VerifyMethod(MethodInfo interfaceMethod)
    {
        if (!typeof(Task).IsAssignableFrom(interfaceMethod.ReturnType))
        {
            throw new InvalidOperationException(
                $"Cannot generate proxy implementation for '{typeof(T).FullName}.{interfaceMethod.Name}'. All client proxy methods must return '{typeof(Task).FullName}' or '{typeof(Task).FullName}<T>'.");
        }

        foreach (var parameter in interfaceMethod.GetParameters())
        {
            if (parameter.IsOut)
            {
                throw new InvalidOperationException(
                    $"Cannot generate proxy implementation for '{typeof(T).FullName}.{interfaceMethod.Name}'. Client proxy methods must not have 'out' parameters.");
            }

            if (parameter.ParameterType.IsByRef)
            {
                throw new InvalidOperationException(
                    $"Cannot generate proxy implementation for '{typeof(T).FullName}.{interfaceMethod.Name}'. Client proxy methods must not have 'ref' parameters.");
            }
        }
    }
}
