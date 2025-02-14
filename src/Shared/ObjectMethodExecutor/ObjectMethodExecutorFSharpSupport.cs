// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Extensions.Internal;

/// <summary>
/// Helper for converting F# async values to their BCL analogues.
/// </summary>
/// <remarks>
/// The main design goal here is to avoid taking a compile-time dependency on
/// FSharp.Core.dll, because non-F# applications wouldn't use it. So all the references
/// to FSharp types have to be constructed dynamically at runtime.
/// </remarks>
[RequiresDynamicCode("Dynamically generates calls to FSharpAsync.")]
internal static class ObjectMethodExecutorFSharpSupport
{
    private const string FSharpAsyncGenericTypeName = "Microsoft.FSharp.Control.FSharpAsync`1";
    private const string FSharpAsyncTypeName = "Microsoft.FSharp.Control.FSharpAsync";
    private const string FSharpUnitTypeName = "Microsoft.FSharp.Core.Unit";
    private const string FSharpOptionTypeName = "Microsoft.FSharp.Core.FSharpOption`1";

    private static readonly object _fsharpValuesCacheLock = new object();
    private static Assembly _fsharpCoreAssembly;
    private static MethodInfo _fsharpAsyncStartAsTaskGenericMethod;
    private static PropertyInfo _fsharpOptionOfTaskCreationOptionsNoneProperty;
    private static PropertyInfo _fsharpOptionOfCancellationTokenNoneProperty;

    /// <summary>
    /// Builds a <see cref="LambdaExpression"/> for converting a value of the given generic instantiation of
    /// <see href="https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync-1.html">FSharp.Control.FSharpAsync&lt;T&gt;</see>
    /// to a <see cref="Task{TResult}"/>, if <paramref name="possibleFSharpAsyncType"/> is in fact a closed F# async type,
    /// or to a <see cref="Task"/>, if <c>TResult</c> is <see href="https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-unit-0.html">FSharp.Core.Unit</see>.
    /// </summary>
    /// <param name="possibleFSharpAsyncType">
    /// The type that is a potential generic instantiation of
    /// <see href="https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync-1.html">FSharp.Control.FSharpAsync&lt;T&gt;</see>.
    /// </param>
    /// <param name="coerceToAwaitableExpression">
    /// When this method returns and <paramref name="possibleFSharpAsyncType"/> is a generic instantiation of
    /// <see href="https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync-1.html">FSharp.Control.FSharpAsync&lt;T&gt;</see>,
    /// contains a <see cref="LambdaExpression"/> for converting a value of type <paramref name="possibleFSharpAsyncType"/>
    /// to a <see cref="Task{TResult}"/>, or to a <see cref="Task"/>, if <c>TResult</c> is <see href="https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-unit-0.html">FSharp.Core.Unit</see>;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <param name="awaitableType">
    /// When this method returns, contains the type of the closed generic instantiation of <see cref="Task{TResult}"/> or of <see cref="Task"/> that will be returned
    /// by the coercer expression, if it was possible to build a coercer; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if it was possible to build a coercer; otherwise, <see langword="false"/>.</returns>
    [RequiresUnreferencedCode("Reflecting over the async FSharpAsync<> contract.")]
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

        // coerceToAwaitableExpression = (FSharpAsync<TResult> fsharpAsync) =>
        // {
        //     return FSharpAsync.StartAsTask<TResult>(
        //         fsharpAsync,
        //         FSharpOption<TaskCreationOptions>.None,
        //         FSharpOption<CancellationToken>.None);
        // };

        var startAsTaskClosedMethod = _fsharpAsyncStartAsTaskGenericMethod
            .MakeGenericMethod(awaiterResultType);

        var coerceToAwaitableParam = Expression.Parameter(possibleFSharpAsyncType);

        var startAsTaskCall =
            Expression.Call(
                method: startAsTaskClosedMethod,
                arg0: coerceToAwaitableParam,
                arg1: Expression.MakeMemberAccess(null, _fsharpOptionOfTaskCreationOptionsNoneProperty),
                arg2: Expression.MakeMemberAccess(null, _fsharpOptionOfCancellationTokenNoneProperty));

        Expression body =
            TryBuildCoercerFromUnitAwaitableToVoidAwaitable(awaitableType, out var coercerExpression, out var nonGenericAwaitableType)
                ? Expression.Invoke(coercerExpression, startAsTaskCall)
                : startAsTaskCall;

        coerceToAwaitableExpression = Expression.Lambda(body, coerceToAwaitableParam);

        if (nonGenericAwaitableType is not null)
        {
            awaitableType = nonGenericAwaitableType;
        }

        return true;
    }

    /// <summary>
    /// Builds a <see cref="LambdaExpression"/> for converting a <c>Task&lt;unit&gt;</c> or <c>ValueTask&lt;unit&gt;</c>
    /// to a void-returning <see cref="Task"/> or <see cref="ValueTask"/>.
    /// </summary>
    /// <param name="genericAwaitableType">The generic awaitable type to convert.</param>
    /// <param name="coercerExpression">
    /// When this method returns and the <paramref name="genericAwaitableType"/> was
    /// <c>Task&lt;unit&gt;</c> or <c>ValueTask&lt;unit&gt;</c>,
    /// contains a <see cref="LambdaExpression"/> for converting to the corresponding void-returning awaitable type;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <param name="nonGenericAwaitableType">
    /// When this method returns and the <paramref name="genericAwaitableType"/> was
    /// <c>Task&lt;unit&gt;</c> or <c>ValueTask&lt;unit&gt;</c>,
    /// contains the corresponding void-returning awaitable type;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if it was possible to build a coercer; otherwise, <see langword="false"/>.</returns>
    [RequiresUnreferencedCode("Reflecting over FSharp.Core.Unit.")]
    public static bool TryBuildCoercerFromUnitAwaitableToVoidAwaitable(
        Type genericAwaitableType,
        out Expression coercerExpression,
        out Type nonGenericAwaitableType)
    {
        if (!genericAwaitableType.IsGenericType)
        {
            coercerExpression = null;
            nonGenericAwaitableType = null;
            return false;
        }

        (nonGenericAwaitableType, coercerExpression) = genericAwaitableType.GetGenericTypeDefinition() switch
        {
            var typeDef when typeDef == typeof(Task<>) && IsFSharpUnit(genericAwaitableType.GetGenericArguments()[0]) => (typeof(Task), MakeTaskOfUnitToTaskExpression(genericAwaitableType)),
            var typeDef when typeDef == typeof(ValueTask<>) && IsFSharpUnit(genericAwaitableType.GetGenericArguments()[0]) => (typeof(ValueTask), MakeValueTaskOfUnitToValueTaskExpression(genericAwaitableType)),
            _ => default
        };

        return (nonGenericAwaitableType, coercerExpression) is ({ }, { });

        static Expression MakeTaskOfUnitToTaskExpression(Type type)
        {
            var closedGenericTaskParam = Expression.Parameter(type);
            return Expression.Lambda(Expression.Convert(closedGenericTaskParam, typeof(Task)), closedGenericTaskParam);
        }

        static Expression MakeValueTaskOfUnitToValueTaskExpression(Type type)
        {
            var closedGenericTaskParam = Expression.Parameter(type);

            var conversionMethod =
                typeof(ObjectMethodExecutorFSharpSupport)
                    .GetMethod(nameof(ConvertValueTaskOfTToValueTask), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(type.GetGenericArguments());

            return Expression.Lambda(Expression.Call(conversionMethod, closedGenericTaskParam), closedGenericTaskParam);
        }
    }

    [RequiresUnreferencedCode("Reflecting over the async FSharpAsync<> contract.")]
    private static bool IsFSharpAsyncOpenGenericType(Type possibleFSharpAsyncType) =>
        IsCoerceableFSharpType(possibleFSharpAsyncType, FSharpAsyncGenericTypeName);

    [RequiresUnreferencedCode("Reflecting over the async FSharpAsync<> contract.")]
    private static bool IsFSharpUnit(Type possibleFSharpUnitType) =>
        IsCoerceableFSharpType(possibleFSharpUnitType, FSharpUnitTypeName);

    [RequiresUnreferencedCode("Reflecting over the async FSharpAsync<> contract.")]
    private static bool IsCoerceableFSharpType(Type possibleFSharpType, string coerceableFSharpTypeName)
    {
        var typeFullName = possibleFSharpType?.FullName;
        if (!string.Equals(typeFullName, coerceableFSharpTypeName, StringComparison.Ordinal))
        {
            return false;
        }

        lock (_fsharpValuesCacheLock)
        {
            if (_fsharpCoreAssembly != null)
            {
                // Since we've already found the real FSharp.Core assembly, we just have
                // to check that the supplied type is the one from that assembly.
                return possibleFSharpType.Assembly == _fsharpCoreAssembly;
            }
            else
            {
                // We'll keep trying to find the F# types/values each time any type
                // with a name of interest is supplied.
                return TryPopulateFSharpValueCaches(possibleFSharpType);
            }
        }
    }

    [RequiresUnreferencedCode("Reflecting over the async FSharpAsync<> contract.")]
    private static bool TryPopulateFSharpValueCaches(Type possibleFSharpType)
    {
        var assembly = possibleFSharpType.Assembly;
        var fsharpOptionType = assembly.GetType(FSharpOptionTypeName);
        var fsharpAsyncType = assembly.GetType(FSharpAsyncTypeName);
        var fsharpAsyncGenericType = assembly.GetType(FSharpAsyncGenericTypeName);

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
                && TypesHaveSameIdentity(parameters[0].ParameterType, fsharpAsyncGenericType)
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

    private static async ValueTask ConvertValueTaskOfTToValueTask<T>(ValueTask<T> valueTask) => await valueTask;
}
