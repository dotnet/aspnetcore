// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
