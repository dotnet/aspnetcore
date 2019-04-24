// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http.Internal
{
    internal class PathStringHelper
    {
        private static readonly bool[] ValidPathChars = {
            false, false, false, false, false, false, false, false,     // 0x00 - 0x07
            false, false, false, false, false, false, false, false,     // 0x08 - 0x0F
            false, false, false, false, false, false, false, false,     // 0x10 - 0x17
            false, false, false, false, false, false, false, false,     // 0x18 - 0x1F
            false, true,  false, false, true,  false, true,  true,      // 0x20 - 0x27
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x28 - 0x2F
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x30 - 0x37
            true,  true,  true,  true,  false, true,  false, false,     // 0x38 - 0x3F
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x40 - 0x47
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x48 - 0x4F
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x50 - 0x57
            true,  true,  true,  false, false, false, false, true,      // 0x58 - 0x5F
            false, true,  true,  true,  true,  true,  true,  true,      // 0x60 - 0x67
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x68 - 0x6F
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x70 - 0x77
            true,  true,  true,  false, false, false, true,  false,     // 0x78 - 0x7F
        };

        // Map from an ASCII char to its hex value, e.g. arr['b'] == 11. 0xFF means it's not a hex digit.
        // From coreclr/src/System.Private.CoreLib/shared/System/Number.Parsing.cs
        // byte[] rather than ReadOnlySpan<byte> to avoid double .Length check https://github.com/dotnet/coreclr/issues/24209
        private static readonly byte[] CharToHexLookup = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 15
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 31
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 47
            0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 63
            0xFF, 0xA,  0xB,  0xC,  0xD,  0xE,  0xF,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 79
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 95
            0xFF, 0xa,  0xb,  0xc,  0xd,  0xe,  0xf // 102
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidPathChar(char c)
        {
            // Use local array and uint .Length compare to elide the bounds check on array access
            var validChars = ValidPathChars;
            var i = (int)c;
            return (uint)i < (uint)validChars.Length && validChars[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPercentEncodedChar(string str, int index)
        {
            if (str[index] == '%' && index < str.Length - 2)
            {
                return AreFollowingTwoCharsHex(str, index);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AreFollowingTwoCharsHex(string str, int index)
        {
            Debug.Assert(index < str.Length - 2);

            var charToHex = CharToHexLookup;
            var i1 = (int)str[index + 1];
            var i2 = (int)str[index + 2];
            if ((uint)i1 < (uint)charToHex.Length &&
                charToHex[i1] != 0xff &&
               (uint)i2 < (uint)charToHex.Length &&
                charToHex[i2] != 0xff)
            {
                return true;
            }

            return false;
        }
    }
}
