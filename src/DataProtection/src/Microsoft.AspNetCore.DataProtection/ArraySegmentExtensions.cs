// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection
{
    internal static class ArraySegmentExtensions
    {
        public static byte[] AsStandaloneArray(this ArraySegment<byte> arraySegment)
        {
            // Fast-track: Don't need to duplicate the array.
            if (arraySegment.Offset == 0 && arraySegment.Count == arraySegment.Array.Length)
            {
                return arraySegment.Array;
            }

            var retVal = new byte[arraySegment.Count];
            Buffer.BlockCopy(arraySegment.Array, arraySegment.Offset, retVal, 0, retVal.Length);
            return retVal;
        }

        public static void Validate<T>(this ArraySegment<T> arraySegment)
        {
            // Since ArraySegment<T> is a struct, it can be improperly initialized or torn.
            // We call the ctor again to make sure the instance data is valid.
            var unused = new ArraySegment<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }
    }
}
