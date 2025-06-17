// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Reflection;

internal sealed class PropertyGetter
{
    private static readonly MethodInfo CallPropertyGetterOpenGenericMethod =
        typeof(PropertyGetter).GetMethod(nameof(CallPropertyGetter), BindingFlags.NonPublic | BindingFlags.Static)!;
    
    private static readonly MethodInfo CallPropertyGetterByReferenceOpenGenericMethod =
        typeof(PropertyGetter).GetMethod(nameof(CallPropertyGetterByReference), BindingFlags.NonPublic | BindingFlags.Static)!;

    // Delegate type for a by-ref property getter
    private delegate TValue ByRefFunc<TDeclaringType, TValue>(ref TDeclaringType arg);

    private readonly Func<object, object?> _GetterDelegate;

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2060:MakeGenericMethod",
        Justification = "The referenced methods don't have any DynamicallyAccessedMembers annotations. See https://github.com/mono/linker/issues/1727")]
    public PropertyGetter(Type targetType, PropertyInfo property)
    {
        if (property.GetMethod == null)
        {
            throw new InvalidOperationException("Cannot provide a value for property " +
                $"'{property.Name}' on type '{targetType.FullName}' because the property " +
                "has no getter.");
        }

        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            var getMethod = property.GetMethod;

            // Instance methods in the CLR can be turned into static methods where the first parameter
            // is open over "target". This parameter is always passed by reference, so we have a code
            // path for value types and a code path for reference types.
            if (getMethod.DeclaringType!.IsValueType)
            {
                // Create a delegate (ref TDeclaringType) -> TValue
                var delegateType = typeof(ByRefFunc<,>).MakeGenericType(targetType, property.PropertyType);
                var propertyGetterDelegate = getMethod.CreateDelegate(delegateType);
                var wrapperDelegateMethod = CallPropertyGetterByReferenceOpenGenericMethod.MakeGenericMethod(targetType, property.PropertyType);
                var accessorDelegate = wrapperDelegateMethod.CreateDelegate(
                    typeof(Func<object, object?>),
                    propertyGetterDelegate);
                _GetterDelegate = (Func<object, object?>)accessorDelegate;
            }
            else
            {
                // Create a delegate TDeclaringType -> TValue
                var delegateType = typeof(Func<,>).MakeGenericType(targetType, property.PropertyType);
                var propertyGetterDelegate = getMethod.CreateDelegate(delegateType);
                var wrapperDelegateMethod = CallPropertyGetterOpenGenericMethod.MakeGenericMethod(targetType, property.PropertyType);
                var accessorDelegate = wrapperDelegateMethod.CreateDelegate(
                    typeof(Func<object, object?>),
                    propertyGetterDelegate);
                _GetterDelegate = (Func<object, object?>)accessorDelegate;
            }
        }
        else
        {
            _GetterDelegate = property.GetValue;
        }
    }

    public object? GetValue(object target) => _GetterDelegate(target);

    private static object? CallPropertyGetter<TTarget, TValue>(
        Func<TTarget, TValue> Getter,
        object target)
        where TTarget : notnull
    {
        Console.WriteLine($"CallPropertyGetter called: TTarget={typeof(TTarget)}, TValue={typeof(TValue)}, target={target}");
        var result = Getter((TTarget)target);
        Console.WriteLine($"CallPropertyGetter result: {result}");
        return result;
    }

    private static object? CallPropertyGetterByReference<TTarget, TValue>(
        ByRefFunc<TTarget, TValue> Getter,
        object target)
        where TTarget : notnull
    {
        Console.WriteLine($"CallPropertyGetterByReference called: TTarget={typeof(TTarget)}, TValue={typeof(TValue)}, target={target}");
        var unboxed = (TTarget)target;
        var result = Getter(ref unboxed);
        Console.WriteLine($"CallPropertyGetterByReference result: {result}");
        return result;
    }
}
