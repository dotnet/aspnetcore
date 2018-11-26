// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class IntPtrHelper
    {
        internal static IntPtr Add(IntPtr a, int b)
        {
            return (IntPtr)((long)a + (long)b);
        }

        internal static long Subtract(IntPtr a, IntPtr b)
        {
            return ((long)a - (long)b);
        }
    }
}
