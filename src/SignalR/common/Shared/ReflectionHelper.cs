// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR;

internal static class ReflectionHelper
{
    // mustBeDirectType - Hub methods must use the base 'stream' type and not be a derived class that just implements the 'stream' type
    // and 'stream' types from the client are allowed to inherit from accepted 'stream' types
    public static bool IsStreamingType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type, bool mustBeDirectType = false)
    {
        // TODO #2594 - add Streams here, to make sending files easy

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

    public static bool IsIAsyncEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                return true;
            }
        }

        return type.GetInterfaces().Any(t =>
        {
            if (t.IsGenericType)
            {
                return t.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);
            }
            else
            {
                return false;
            }
        });
    }
}
