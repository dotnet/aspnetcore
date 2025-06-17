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
                var propertyGetterAsFunc =
                    getMethod.CreateDelegate(typeof(ByRefFunc<,>).MakeGenericType(targetType, property.PropertyType));
                var callPropertyGetterClosedGenericMethod =
                    CallPropertyGetterByReferenceOpenGenericMethod.MakeGenericMethod(targetType, property.PropertyType);
                _GetterDelegate = (Func<object, object?>)
                    callPropertyGetterClosedGenericMethod.CreateDelegate(typeof(Func<object, object?>), propertyGetterAsFunc);
            }
            else
            {
                // Create a delegate TDeclaringType -> TValue
                var propertyGetterAsFunc =
                    getMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(targetType, property.PropertyType));
                var callPropertyGetterClosedGenericMethod =
                    CallPropertyGetterOpenGenericMethod.MakeGenericMethod(targetType, property.PropertyType);
                _GetterDelegate = (Func<object, object?>)
                    callPropertyGetterClosedGenericMethod.CreateDelegate(typeof(Func<object, object?>), propertyGetterAsFunc);
            }
        }
        else
        {
            _GetterDelegate = property.GetValue;
        }
    }

    public object? GetValue(object target) => _GetterDelegate(target);

    private static TValue CallPropertyGetter<TTarget, TValue>(
        Func<TTarget, TValue> Getter,
        object target)
        where TTarget : notnull
    {
        return Getter((TTarget)target);
    }

    private static TValue CallPropertyGetterByReference<TTarget, TValue>(
        ByRefFunc<TTarget, TValue> Getter,
        object target)
        where TTarget : notnull
    {
        var unboxed = (TTarget)target;
        return Getter(ref unboxed);
    }
}
