// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Contains helpers for dealing with byte-hex char conversions.
    /// </summary>
    internal static class HexUtil
    {
        /// <summary>
        /// Converts a number 0 - 15 to its associated hex character '0' - 'F'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char IntToChar(uint i)
        {
            Debug.Assert(i < 16);
            return (i < 10) ? (char)('0' + i) : (char)('A' + (i - 10));
        }

        /// <summary>
        /// Returns the integral form of this hexadecimal character.
        /// </summary>
        /// <returns>0 - 15 if the character is valid, -1 if the character is invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ParseHexCharacter(char c)
        {
            if ('0' <= c && c <= '9') { return c - '0'; }
            else if ('A' <= c && c <= 'F') { return c - 'A' + 10; }
            else if ('a' <= c && c <= 'f') { return c - 'a' + 10; }
            else { return -1; }
        }

        /// <summary>
        /// Gets the uppercase hex-encoded form of a byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteHexEncodedByte(byte b, out char firstHexChar, out char secondHexChar)
        {
            firstHexChar = IntToChar((uint)b >> 4);
            secondHexChar = IntToChar((uint)b & 0xFU);
        }
    }
}
