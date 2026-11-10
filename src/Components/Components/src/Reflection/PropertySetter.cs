// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Reflection;

internal sealed partial class PropertySetter
{
    internal bool IsEventCallbackSetter { get; private set; }

    [RequiresDynamicCode("Creating a closed generic property setter requires dynamic code.")]
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2060:MakeGenericMethod",
        Justification = "The referenced methods don't have any DynamicallyAccessedMembers annotations.")]
    partial void GetCustomPropertySetter(Type targetType, Type propertyType, ref MethodInfo setterMethod)
    {
        if (propertyType == typeof(EventCallback))
        {
            setterMethod = typeof(PropertySetter)
                .GetMethod(nameof(CallEventCallbackPropertySetter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(targetType);
            IsEventCallbackSetter = true;
        }
        else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(EventCallback<>))
        {
            setterMethod = typeof(PropertySetter)
                .GetMethod(nameof(CallEventCallbackOfTPropertySetter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(targetType, propertyType.GenericTypeArguments[0]);
            IsEventCallbackSetter = true;
        }
    }

    private static void CallEventCallbackPropertySetter<TTarget>(
        Action<TTarget, EventCallback> setter,
        object target,
        object value)
        where TTarget : notnull
    {
        var callback = value is null ? default : (EventCallback)value;
        if (callback.IsBinding)
        {
            callback = new EventCallback(callback.Receiver, callback.Delegate);
        }

        setter((TTarget)target, callback);
    }

    private static void CallEventCallbackOfTPropertySetter<TTarget, TValue>(
        Action<TTarget, EventCallback<TValue>> setter,
        object target,
        object value)
        where TTarget : notnull
    {
        var callback = value is null ? default : (EventCallback<TValue>)value;
        if (callback.IsBinding)
        {
            callback = new EventCallback<TValue>(callback.Receiver, callback.Delegate);
        }

        setter((TTarget)target, callback);
    }
}
