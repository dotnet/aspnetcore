// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.Streams;

internal static class Requires
{
    internal static void NotNull(object arg, string paramName)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    internal static void Argument(bool condition, string paramName, string message)
    {
        if (condition)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    internal static void Range(bool condition, string paramName)
    {
        if (condition)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }
}
