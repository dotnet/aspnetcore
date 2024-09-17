// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Core.Infrastructure;

internal static class AsyncEnumerableHelper
{
    internal static bool IsIAsyncEnumerable(Type type) => GetIAsyncEnumerableInterface(type) is not null;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "The 'IAsyncEnumerable<>' Type must exist (due to typeof(IAsyncEnumerable<>) used below)" +
            " and so the trimmer kept it. In which case " +
            "It also kept it on any type which implements it. The below call to GetInterfaces " +
            "may return fewer results when trimmed but it will return 'IAsyncEnumerable<>' " +
            "if the type implemented it, even after trimming.")]
    private static Type? GetIAsyncEnumerableInterface(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            return type;
        }

        if (type.GetInterface("IAsyncEnumerable`1") is Type asyncEnumerableType)
        {
            return asyncEnumerableType;
        }

        return null;
    }
}
