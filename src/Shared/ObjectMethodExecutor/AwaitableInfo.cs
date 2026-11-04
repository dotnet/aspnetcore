// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Internal;

internal readonly struct AwaitableInfo
{
    internal const string RequiresUnreferencedCodeMessage = "Uses unbounded reflection to determine awaitability of types.";

    private const BindingFlags Everything = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
    private static readonly MethodInfo INotifyCompletion_OnCompleted = typeof(INotifyCompletion).GetMethod(nameof(INotifyCompletion.OnCompleted), Everything, new[] { typeof(Action) })!;
    private static readonly MethodInfo ICriticalNotifyCompletion_UnsafeOnCompleted = typeof(ICriticalNotifyCompletion).GetMethod(nameof(ICriticalNotifyCompletion.UnsafeOnCompleted), Everything, new[] { typeof(Action) })!;

    public Type AwaiterType { get; }
    public PropertyInfo AwaiterIsCompletedProperty { get; }
    public MethodInfo AwaiterGetResultMethod { get; }
    public MethodInfo AwaiterOnCompletedMethod { get; }
    public MethodInfo? AwaiterUnsafeOnCompletedMethod { get; }
    public Type ResultType { get; }
    public MethodInfo GetAwaiterMethod { get; }

    public AwaitableInfo(
        Type awaiterType,
        PropertyInfo awaiterIsCompletedProperty,
        MethodInfo awaiterGetResultMethod,
        MethodInfo awaiterOnCompletedMethod,
        MethodInfo? awaiterUnsafeOnCompletedMethod,
        Type resultType,
        MethodInfo getAwaiterMethod)
    {
        AwaiterType = awaiterType;
        AwaiterIsCompletedProperty = awaiterIsCompletedProperty;
        AwaiterGetResultMethod = awaiterGetResultMethod;
        AwaiterOnCompletedMethod = awaiterOnCompletedMethod;
        AwaiterUnsafeOnCompletedMethod = awaiterUnsafeOnCompletedMethod;
        ResultType = resultType;
        GetAwaiterMethod = getAwaiterMethod;
    }

    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static bool IsTypeAwaitable(
        Type type,
        out AwaitableInfo awaitableInfo)
    {
        // Based on Roslyn code: http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Shared/Extensions/ISymbolExtensions.cs,db4d48ba694b9347

        // Awaitable must have method matching "object GetAwaiter()"
        var getAwaiterMethod = type.GetMethod("GetAwaiter", Everything, Type.EmptyTypes);

        if (getAwaiterMethod is null)
        {
            awaitableInfo = default(AwaitableInfo);
            return false;
        }

        var awaiterType = getAwaiterMethod.ReturnType;

        // Awaiter must have property matching "bool IsCompleted { get; }"
        var isCompletedProperty = awaiterType.GetProperty("IsCompleted", Everything, binder: null, returnType: typeof(bool), types: Type.EmptyTypes, modifiers: null);
        if (isCompletedProperty is null)
        {
            awaitableInfo = default(AwaitableInfo);
            return false;
        }

        // Awaiter must implement INotifyCompletion
        var implementsINotifyCompletion = typeof(INotifyCompletion).IsAssignableFrom(awaiterType);
        if (!implementsINotifyCompletion)
        {
            awaitableInfo = default(AwaitableInfo);
            return false;
        }

        // INotifyCompletion supplies a method matching "void OnCompleted(Action action)"
        var onCompletedMethod = INotifyCompletion_OnCompleted;

        // Awaiter optionally implements ICriticalNotifyCompletion
        var implementsICriticalNotifyCompletion = typeof(ICriticalNotifyCompletion).IsAssignableFrom(awaiterType);
        MethodInfo? unsafeOnCompletedMethod = null;
        if (implementsICriticalNotifyCompletion)
        {
            // ICriticalNotifyCompletion supplies a method matching "void UnsafeOnCompleted(Action action)"
            unsafeOnCompletedMethod = ICriticalNotifyCompletion_UnsafeOnCompleted;
        }

        // Awaiter must have method matching "void GetResult" or "T GetResult()"
        var getResultMethod = awaiterType.GetMethod("GetResult", Everything, Type.EmptyTypes);

        if (getResultMethod is null)
        {
            awaitableInfo = default(AwaitableInfo);
            return false;
        }

        awaitableInfo = new AwaitableInfo(
            awaiterType,
            isCompletedProperty,
            getResultMethod,
            onCompletedMethod,
            unsafeOnCompletedMethod,
            getResultMethod.ReturnType,
            getAwaiterMethod);
        return true;
    }
}
