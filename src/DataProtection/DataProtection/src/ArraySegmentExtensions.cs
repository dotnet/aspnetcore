// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.DataProtection;

internal static class ArraySegmentExtensions
{
    public static byte[] AsStandaloneArray(this ArraySegment<byte> arraySegment)
    {
        // Fast-track: Don't need to duplicate the array.
        if (arraySegment.Offset == 0 && arraySegment.Count == arraySegment.Array!.Length)
        {
            return arraySegment.Array;
        }

        var retVal = new byte[arraySegment.Count];
        Buffer.BlockCopy(arraySegment.Array!, arraySegment.Offset, retVal, 0, retVal.Length);
        return retVal;
    }

    public static void Validate<T>(this ArraySegment<T> arraySegment)
    {
        // Since ArraySegment<T> is a struct, it can be improperly initialized or torn.
        // We call the ctor again to make sure the instance data is valid.
        _ = new ArraySegment<T>(arraySegment.Array!, arraySegment.Offset, arraySegment.Count);
    }
}
