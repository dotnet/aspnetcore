// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Shared;

internal static class Int7BitEncodingUtils
{
    public static int Measure7BitEncodedUIntLength(this int value)
        => Measure7BitEncodedUIntLength((uint)value);

    public static int Measure7BitEncodedUIntLength(this uint value)
    {
#if NET10_0_OR_GREATER
        return ((31 - System.Numerics.BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;
#else
        int count = 1;
        while ((value >>= 7) != 0)
        {
            count++;
        }
        return count;
#endif
    }

    public static int Write7BitEncodedInt(this Span<byte> target, int value)
        => Write7BitEncodedInt(target, (uint)value);

    public static int Write7BitEncodedInt(this Span<byte> target, uint uValue)
    {
        // Write out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        //
        // Using the constants 0x7F and ~0x7F below offers smaller
        // codegen than using the constant 0x80.

        int index = 0;
        while (uValue > 0x7Fu)
        {
            target[index++] = (byte)(uValue | ~0x7Fu);
            uValue >>= 7;
        }

        target[index++] = (byte)uValue;
        return index;
    }
}
