// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR
{
    internal static class ReflectionHelper
    {
        // mustBeDirectType - Hub methods must use the base 'stream' type and not be a derived class that just implements the 'stream' type
        // and 'stream' types from the client are allowed to inherit from accepted 'stream' types
        public static bool IsStreamingType(Type type, bool mustBeDirectType = false)
        {
            // TODO #2594 - add Streams here, to make sending files easy

            if (IsIAsyncEnumerable(type))
            {
                return true;
            }

            Type? nullableType = type;

            do
            {
                if (nullableType.IsGenericType && nullableType.GetGenericTypeDefinition() == typeof(ChannelReader<>))
                {
                    return true;
                }

                nullableType = nullableType.BaseType;
            } while (mustBeDirectType == false && nullableType != null);

            return false;
        }

        public static bool IsIAsyncEnumerable(Type type)
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
}
