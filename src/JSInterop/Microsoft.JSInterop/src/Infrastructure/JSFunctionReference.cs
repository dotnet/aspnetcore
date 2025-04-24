// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// TODO(OR): Document this.
/// </summary>
internal readonly struct JSFunctionReference
{
    /// <summary>
    /// Caches previously constructed MethodInfo instances for various delegate types.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, MethodInfo> _methodInfoCache = new();

    private readonly IJSObjectReference _jsObjectReference;

    public JSFunctionReference(IJSObjectReference jsObjectReference)
    {
        _jsObjectReference = jsObjectReference;
    }

    /// <summary>
    /// TODO(OR): Document this.
    /// </summary>
    public static T CreateInvocationDelegate<T>(IJSObjectReference jsObjectReference) where T : Delegate
    {
        Type delegateType = typeof(T);

        if (_methodInfoCache.TryGetValue(delegateType, out var wrapperMethod))
        {
            var wrapper = new JSFunctionReference(jsObjectReference);
            return (T)Delegate.CreateDelegate(delegateType, wrapper, wrapperMethod);
        }

        if (!delegateType.IsGenericType)
        {
            throw CreateInvalidTypeParameterException(delegateType);
        }

        var returnTypeCandidate = delegateType.GenericTypeArguments[^1];

        if (returnTypeCandidate == typeof(ValueTask))
        {
            var methodName = GetVoidMethodName(delegateType);
            return CreateVoidDelegate<T>(delegateType, jsObjectReference, methodName);
        }
        else if (returnTypeCandidate == typeof(Task))
        {
            var methodName = GetVoidTaskMethodName(delegateType);
            return CreateVoidDelegate<T>(delegateType, jsObjectReference, methodName);
        }
        else if (returnTypeCandidate.IsGenericType)
        {
            var returnTypeGenericTypeDefinition = returnTypeCandidate.GetGenericTypeDefinition();

            if (returnTypeGenericTypeDefinition == typeof(ValueTask<>))
            {
                var methodName = GetMethodName(delegateType);
                var innerReturnType = returnTypeCandidate.GenericTypeArguments[0];
                return CreateDelegate<T>(delegateType, innerReturnType, jsObjectReference, methodName);
            }

            else if (returnTypeGenericTypeDefinition == typeof(Task<>))
            {
                var methodName = GetTaskMethodName(delegateType);
                var innerReturnType = returnTypeCandidate.GenericTypeArguments[0];
                return CreateDelegate<T>(delegateType, innerReturnType, jsObjectReference, methodName);
            }
        }

        throw CreateInvalidTypeParameterException(delegateType);
    }

    private static T CreateDelegate<T>(Type delegateType, Type returnType, IJSObjectReference jsObjectReference, string methodName) where T : Delegate
    {
        var wrapperMethod = typeof(JSFunctionReference).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Type[] genericArguments = [.. delegateType.GenericTypeArguments[..^1], returnType];

#pragma warning disable IL2060 // Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.
        var concreteWrapperMethod = wrapperMethod.MakeGenericMethod(genericArguments);
#pragma warning restore IL2060 // Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.

        _methodInfoCache.TryAdd(delegateType, concreteWrapperMethod);

        var wrapper = new JSFunctionReference(jsObjectReference);
        return (T)Delegate.CreateDelegate(delegateType, wrapper, concreteWrapperMethod);
    }

    private static T CreateVoidDelegate<T>(Type delegateType, IJSObjectReference jsObjectReference, string methodName) where T : Delegate
    {
        var wrapperMethod = typeof(JSFunctionReference).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Type[] genericArguments = delegateType.GenericTypeArguments[..^1];

#pragma warning disable IL2060 // Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.
        var concreteWrapperMethod = wrapperMethod.MakeGenericMethod(genericArguments);
#pragma warning restore IL2060 // Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.

        _methodInfoCache.TryAdd(delegateType, concreteWrapperMethod);

        var wrapper = new JSFunctionReference(jsObjectReference);
        return (T)Delegate.CreateDelegate(delegateType, wrapper, concreteWrapperMethod);
    }

    private static InvalidOperationException CreateInvalidTypeParameterException(Type delegateType)
    {
        return new InvalidOperationException(
            $"The type {delegateType} is not supported as the type parameter of '{nameof(JSObjectReferenceExtensions.AsAsyncFunction)}'. 'T' must be Func with the return type Task<TResult> or ValueTask<TResult>.");
    }

    private static string GetMethodName(Type delegateType) => delegateType.GetGenericTypeDefinition() switch
    {
        var gd when gd == typeof(Func<>) => nameof(Invoke0),
        var gd when gd == typeof(Func<,>) => nameof(Invoke1),
        var gd when gd == typeof(Func<,,>) => nameof(Invoke2),
        var gd when gd == typeof(Func<,,,>) => nameof(Invoke3),
        var gd when gd == typeof(Func<,,,,>) => nameof(Invoke4),
        var gd when gd == typeof(Func<,,,,,>) => nameof(Invoke5),
        var gd when gd == typeof(Func<,,,,,,>) => nameof(Invoke6),
        _ => throw CreateInvalidTypeParameterException(delegateType)
    };

    private static string GetTaskMethodName(Type delegateType) => delegateType.GetGenericTypeDefinition() switch
    {
        var gd when gd == typeof(Func<>) => nameof(InvokeTask0),
        var gd when gd == typeof(Func<,>) => nameof(InvokeTask1),
        var gd when gd == typeof(Func<,,>) => nameof(InvokeTask2),
        var gd when gd == typeof(Func<,,,>) => nameof(InvokeTask3),
        var gd when gd == typeof(Func<,,,,>) => nameof(InvokeTask4),
        var gd when gd == typeof(Func<,,,,,>) => nameof(InvokeTask5),
        var gd when gd == typeof(Func<,,,,,,>) => nameof(InvokeTask6),
        _ => throw CreateInvalidTypeParameterException(delegateType)
    };

    private static string GetVoidMethodName(Type delegateType) => delegateType.GetGenericTypeDefinition() switch
    {
        var gd when gd == typeof(Func<>) => nameof(InvokeVoid0),
        var gd when gd == typeof(Func<,>) => nameof(InvokeVoid1),
        var gd when gd == typeof(Func<,,>) => nameof(InvokeVoid2),
        var gd when gd == typeof(Func<,,,>) => nameof(InvokeVoid3),
        var gd when gd == typeof(Func<,,,,>) => nameof(InvokeVoid4),
        var gd when gd == typeof(Func<,,,,,>) => nameof(InvokeVoid5),
        var gd when gd == typeof(Func<,,,,,,>) => nameof(InvokeVoid6),
        _ => throw CreateInvalidTypeParameterException(delegateType)
    };

    private static string GetVoidTaskMethodName(Type delegateType) => delegateType.GetGenericTypeDefinition() switch
    {
        var gd when gd == typeof(Func<>) => nameof(InvokeVoidTask0),
        var gd when gd == typeof(Func<,>) => nameof(InvokeVoidTask1),
        var gd when gd == typeof(Func<,,>) => nameof(InvokeVoidTask2),
        var gd when gd == typeof(Func<,,,>) => nameof(InvokeVoidTask3),
        var gd when gd == typeof(Func<,,,,>) => nameof(InvokeVoidTask4),
        var gd when gd == typeof(Func<,,,,,>) => nameof(InvokeVoidTask5),
        var gd when gd == typeof(Func<,,,,,,>) => nameof(InvokeVoidTask6),
        _ => throw CreateInvalidTypeParameterException(delegateType)
    };

    // Variants returning ValueTask<T> using InvokeAsync
    public ValueTask<TResult> Invoke0<[DynamicallyAccessedMembers(JsonSerialized)] TResult>() => _jsObjectReference.InvokeAsync<TResult>(string.Empty, []);
    public ValueTask<TResult> Invoke1<T1, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1]);
    public ValueTask<TResult> Invoke2<T1, T2, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2]);
    public ValueTask<TResult> Invoke3<T1, T2, T3, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2, T3 arg3) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3]);
    public ValueTask<TResult> Invoke4<T1, T2, T3, T4, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3, arg4]);
    public ValueTask<TResult> Invoke5<T1, T2, T3, T4, T5, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3, arg4, arg5]);
    public ValueTask<TResult> Invoke6<T1, T2, T3, T4, T5, T6, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3, arg4, arg5, arg6]);

    // Variants returning ValueTask using InvokeVoidAsync
    public ValueTask InvokeVoid0() => _jsObjectReference.InvokeVoidAsync(string.Empty);
    public ValueTask InvokeVoid1<T1>(T1 arg1) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1]);
    public ValueTask InvokeVoid2<T1, T2>(T1 arg1, T2 arg2) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2]);
    public ValueTask InvokeVoid3<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2, arg3]);
    public ValueTask InvokeVoid4<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2, arg3, arg4]);
    public ValueTask InvokeVoid5<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2, arg3, arg4, arg5]);
    public ValueTask InvokeVoid6<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2, arg3, arg4, arg5, arg6]);

    // Variants returning Task<T> using InvokeAsync
    public Task<TResult> InvokeTask0<[DynamicallyAccessedMembers(JsonSerialized)] TResult>() => _jsObjectReference.InvokeAsync<TResult>(string.Empty, []).AsTask();
    public Task<TResult> InvokeTask1<T1, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1]).AsTask();
    public Task<TResult> InvokeTask2<T1, T2, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2]).AsTask();
    public Task<TResult> InvokeTask3<T1, T2, T3, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2, T3 arg3) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3]).AsTask();
    public Task<TResult> InvokeTask4<T1, T2, T3, T4, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3, arg4]).AsTask();
    public Task<TResult> InvokeTask5<T1, T2, T3, T4, T5, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3, arg4, arg5]).AsTask();
    public Task<TResult> InvokeTask6<T1, T2, T3, T4, T5, T6, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => _jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3, arg4, arg5, arg6]).AsTask();

    // Variants returning Task using InvokeVoidAsync
    public Task InvokeVoidTask0() => _jsObjectReference.InvokeVoidAsync(string.Empty).AsTask();
    public Task InvokeVoidTask1<T1>(T1 arg1) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1]).AsTask();
    public Task InvokeVoidTask2<T1, T2>(T1 arg1, T2 arg2) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2]).AsTask();
    public Task InvokeVoidTask3<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2, arg3]).AsTask();
    public Task InvokeVoidTask4<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2, arg3, arg4]).AsTask();
    public Task InvokeVoidTask5<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2, arg3, arg4, arg5]).AsTask();
    public Task InvokeVoidTask6<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => _jsObjectReference.InvokeVoidAsync(string.Empty, [arg1, arg2, arg3, arg4, arg5, arg6]).AsTask();
}
