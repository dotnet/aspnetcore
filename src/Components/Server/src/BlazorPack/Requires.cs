// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;

namespace Nerdbank.Streams
{
    internal static class Requires
    {
        internal static void NotNull(object arg, string paramName)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(paramName));
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
}
