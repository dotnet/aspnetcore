// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR;

internal static class ReflectionHelper
{
    // mustBeDirectType - Hub methods must use the base 'stream' type and not be a derived class that just implements the 'stream' type
    // and 'stream' types from the client are allowed to inherit from accepted 'stream' types
    public static bool IsStreamingType(Type type, bool mustBeDirectType = false)
    {
        // TODO https://github.com/dotnet/aspnetcore/issues/5316 - add Streams here, to make sending files easy

        if (IsIAsyncEnumerable(type))
        {
            return true;
        }

        return TryGetStreamType(type, out _, mustBeDirectType);
    }

    public static bool TryGetStreamType(Type streamType, [NotNullWhen(true)] out Type? streamGenericType, bool mustBeDirectType = false)
    {
        Type? nullableType = streamType;
        do
        {
            if (nullableType.IsGenericType && nullableType.GetGenericTypeDefinition() == typeof(ChannelReader<>))
            {
                Debug.Assert(nullableType.GetGenericArguments().Length == 1);

                streamGenericType = nullableType.GetGenericArguments()[0];
                return true;
            }

            nullableType = nullableType.BaseType;
        } while (mustBeDirectType == false && nullableType != null);

        streamGenericType = null;
        return false;
    }

    public static bool IsIAsyncEnumerable(Type type) => GetIAsyncEnumerableInterface(type) is not null;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "The 'IAsyncEnumerable<>' Type must exist and so trimmer kept it. In which case " +
            "It also kept it on any type which implements it. The below call to GetInterfaces " +
            "may return fewer results when trimmed but it will return 'IAsyncEnumerable<>' " +
            "if the type implemented it, even after trimming.")]
    public static Type? GetIAsyncEnumerableInterface(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            return type;
        }

        foreach (Type typeToCheck in type.GetInterfaces())
        {
            if (typeToCheck.IsGenericType && typeToCheck.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                return typeToCheck;
            }
        }

        return null;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "The 'IAsyncEnumerator<>' Type must exist and so trimmer kept it. In which case " +
            "It also kept it on any type which implements it. The below call to GetInterfaces " +
            "may return fewer results when trimmed but it will return 'IAsyncEnumerator<>' " +
            "if the type implemented it, even after trimming.")]
    public static Type GetIAsyncEnumeratorInterface(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerator<>))
        {
            return type;
        }

        foreach (Type typeToCheck in type.GetInterfaces())
        {
            if (typeToCheck.IsGenericType && typeToCheck.GetGenericTypeDefinition() == typeof(IAsyncEnumerator<>))
            {
                return typeToCheck;
            }
        }

        throw new InvalidOperationException($"Type '{type}' does not implement IAsyncEnumerator<>");
    }
}
