//------------------------------------------------------------------------------
// <copyright file="Internal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.Net.Server
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
