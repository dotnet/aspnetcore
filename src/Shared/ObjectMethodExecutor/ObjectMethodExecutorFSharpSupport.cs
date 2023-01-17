// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Internal;

/// <summary>
/// Helper for detecting whether a given type is FSharpAsync`1, and if so, supplying
/// an <see cref="Expression"/> for mapping instances of that type to a C# awaitable.
/// </summary>
/// <remarks>
/// The main design goal here is to avoid taking a compile-time dependency on
/// FSharp.Core.dll, because non-F# applications wouldn't use it. So all the references
/// to FSharp types have to be constructed dynamically at runtime.
/// </remarks>
[RequiresDynamicCode("Dynamically generates calls to FSharpAsync.")]
internal static class ObjectMethodExecutorFSharpSupport
{
    private static readonly object _fsharpValuesCacheLock = new object();
    private static Assembly _fsharpCoreAssembly;
    private static MethodInfo _fsharpAsyncStartAsTaskGenericMethod;
    private static PropertyInfo _fsharpOptionOfTaskCreationOptionsNoneProperty;
    private static PropertyInfo _fsharpOptionOfCancellationTokenNoneProperty;

    [UnconditionalSuppressMessage("Trimmer", "IL2060", Justification = "Reflecting over the async FSharpAsync<> contract.")]
    public static bool TryBuildCoercerFromFSharpAsyncToAwaitable(
        Type possibleFSharpAsyncType,
        out Expression coerceToAwaitableExpression,
        out Type awaitableType)
    {
        var methodReturnGenericType = possibleFSharpAsyncType.IsGenericType
            ? possibleFSharpAsyncType.GetGenericTypeDefinition()
            : null;

        if (!IsFSharpAsyncOpenGenericType(methodReturnGenericType))
        {
            coerceToAwaitableExpression = null;
            awaitableType = null;
            return false;
        }

        var awaiterResultType = possibleFSharpAsyncType.GetGenericArguments().Single();
        awaitableType = typeof(Task<>).MakeGenericType(awaiterResultType);

        // coerceToAwaitableExpression = (object fsharpAsync) =>
        // {
        //     return (object)FSharpAsync.StartAsTask<TResult>(
        //         (Microsoft.FSharp.Control.FSharpAsync<TResult>)fsharpAsync,
        //         FSharpOption<TaskCreationOptions>.None,
        //         FSharpOption<CancellationToken>.None);
        // };
        var startAsTaskClosedMethod = _fsharpAsyncStartAsTaskGenericMethod
            .MakeGenericMethod(awaiterResultType);
        var coerceToAwaitableParam = Expression.Parameter(typeof(object));
        coerceToAwaitableExpression = Expression.Lambda(
            Expression.Convert(
                Expression.Call(
                    startAsTaskClosedMethod,
                    Expression.Convert(coerceToAwaitableParam, possibleFSharpAsyncType),
                    Expression.MakeMemberAccess(null, _fsharpOptionOfTaskCreationOptionsNoneProperty),
                    Expression.MakeMemberAccess(null, _fsharpOptionOfCancellationTokenNoneProperty)),
                typeof(object)),
            coerceToAwaitableParam);

        return true;
    }

    private static bool IsFSharpAsyncOpenGenericType(Type possibleFSharpAsyncGenericType)
    {
        var typeFullName = possibleFSharpAsyncGenericType?.FullName;
        if (!string.Equals(typeFullName, "Microsoft.FSharp.Control.FSharpAsync`1", StringComparison.Ordinal))
        {
            return false;
        }

        lock (_fsharpValuesCacheLock)
        {
            if (_fsharpCoreAssembly != null)
            {
                // Since we've already found the real FSharpAsync.Core assembly, we just have
                // to check that the supplied FSharpAsync`1 type is the one from that assembly.
                return possibleFSharpAsyncGenericType.Assembly == _fsharpCoreAssembly;
            }
            else
            {
                // We'll keep trying to find the FSharp types/values each time any type called
                // FSharpAsync`1 is supplied.
                return TryPopulateFSharpValueCaches(possibleFSharpAsyncGenericType);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2026", Justification = "Reflecting over the async FSharpAsync<> contract")]
    [UnconditionalSuppressMessage("Trimmer", "IL2055", Justification = "Reflecting over the async FSharpAsync<> contract")]
    [UnconditionalSuppressMessage("Trimmer", "IL2072", Justification = "Reflecting over the async FSharpAsync<> contract")]
    private static bool TryPopulateFSharpValueCaches(Type possibleFSharpAsyncGenericType)
    {
        var assembly = possibleFSharpAsyncGenericType.Assembly;
        var fsharpOptionType = assembly.GetType("Microsoft.FSharp.Core.FSharpOption`1");
        var fsharpAsyncType = assembly.GetType("Microsoft.FSharp.Control.FSharpAsync");

        if (fsharpOptionType == null || fsharpAsyncType == null)
        {
            return false;
        }

        // Get a reference to FSharpOption<TaskCreationOptions>.None
        var fsharpOptionOfTaskCreationOptionsType = fsharpOptionType
            .MakeGenericType(typeof(TaskCreationOptions));
        _fsharpOptionOfTaskCreationOptionsNoneProperty = fsharpOptionOfTaskCreationOptionsType
            .GetRuntimeProperty("None");

        // Get a reference to FSharpOption<CancellationToken>.None
        var fsharpOptionOfCancellationTokenType = fsharpOptionType
            .MakeGenericType(typeof(CancellationToken));
        _fsharpOptionOfCancellationTokenNoneProperty = fsharpOptionOfCancellationTokenType
            .GetRuntimeProperty("None");

        // Get a reference to FSharpAsync.StartAsTask<>
        var fsharpAsyncMethods = fsharpAsyncType
            .GetRuntimeMethods()
            .Where(m => m.Name.Equals("StartAsTask", StringComparison.Ordinal));
        foreach (var candidateMethodInfo in fsharpAsyncMethods)
        {
            var parameters = candidateMethodInfo.GetParameters();
            if (parameters.Length == 3
                && TypesHaveSameIdentity(parameters[0].ParameterType, possibleFSharpAsyncGenericType)
                && parameters[1].ParameterType == fsharpOptionOfTaskCreationOptionsType
                && parameters[2].ParameterType == fsharpOptionOfCancellationTokenType)
            {
                // This really does look like the correct method (and hence assembly).
                _fsharpAsyncStartAsTaskGenericMethod = candidateMethodInfo;
                _fsharpCoreAssembly = assembly;
                break;
            }
        }

        return _fsharpCoreAssembly != null;
    }

    private static bool TypesHaveSameIdentity(Type type1, Type type2)
    {
        return type1.Assembly == type2.Assembly
            && string.Equals(type1.Namespace, type2.Namespace, StringComparison.Ordinal)
            && string.Equals(type1.Name, type2.Name, StringComparison.Ordinal);
    }
}
