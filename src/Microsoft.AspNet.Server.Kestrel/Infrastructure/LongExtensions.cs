// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Server.Kestrel.Extensions
{
    public static class LongExtensions
    {
        public static int BitCount(this long value)
        {
            // Parallel bit count for a 64-bit integer
            var v = (ulong)value;
            v = v - ((v >> 1) & 0x5555555555555555);
            v = (v & 0x3333333333333333) + ((v >> 2) & 0x3333333333333333);
            v = (v + (v >> 4) & 0x0f0f0f0f0f0f0f0f);
            return (int)((v * 0x0101010101010101) >> 56);
        }
    }
}
