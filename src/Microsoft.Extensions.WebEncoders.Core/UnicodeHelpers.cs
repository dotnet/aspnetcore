// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Contains helpers for dealing with Unicode code points.
    /// </summary>
    internal unsafe static class UnicodeHelpers
    {
        /// <summary>
        /// Used for invalid Unicode sequences or other unrepresentable values.
        /// </summary>
        private const char UNICODE_REPLACEMENT_CHAR = '\uFFFD';

        /// <summary>
        /// The last code point defined by the Unicode specification.
        /// </summary>
        internal const int UNICODE_LAST_CODEPOINT = 0x10FFFF;

        private static uint[] _definedCharacterBitmap;

        /// <summary>
        /// Helper method which creates a bitmap of all characters which are
        /// defined per version 8.0 of the Unicode specification.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint[] CreateDefinedCharacterBitmap()
        {
            // The stream should be exactly 8KB in size.
            var assembly = typeof(UnicodeHelpers).GetTypeInfo().Assembly;
            var resourceName = assembly.GetName().Name + ".compiler.resources.unicode-defined-chars.bin";

            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream.Length != 8 * 1024)
            {
                Environment.FailFast("Corrupt data detected.");
            }

            // Read everything in as raw bytes.
            byte[] rawData = new byte[8 * 1024];
            for (int numBytesReadTotal = 0; numBytesReadTotal < rawData.Length;)
            {
                int numBytesReadThisIteration = stream.Read(rawData, numBytesReadTotal, rawData.Length - numBytesReadTotal);
                if (numBytesReadThisIteration == 0)
                {
                    Environment.FailFast("Corrupt data detected.");
                }
                numBytesReadTotal += numBytesReadThisIteration;
            }

            // Finally, convert the byte[] to a uint[].
            // The incoming bytes are little-endian.
            uint[] retVal = new uint[2 * 1024];
            for (int i = 0; i < retVal.Length; i++)
            {
                retVal[i] = (((uint)rawData[4 * i + 3]) << 24)
                    | (((uint)rawData[4 * i + 2]) << 16)
                    | (((uint)rawData[4 * i + 1]) << 8)
                    | (uint)rawData[4 * i];
            }

            // And we're done!
            Volatile.Write(ref _definedCharacterBitmap, retVal);
            return retVal;
        }

        /// <summary>
        /// Returns a bitmap of all characters which are defined per version 8.0
        /// of the Unicode specification.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint[] GetDefinedCharacterBitmap()
        {
            return Volatile.Read(ref _definedCharacterBitmap) ?? CreateDefinedCharacterBitmap();
        }

        /// <summary>
        /// Given a UTF-16 character stream, reads the next scalar value from the stream.
        /// Set 'endOfString' to true if 'pChar' points to the last character in the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetScalarValueFromUtf16(char* pChar, bool endOfString)
        {
            // This method is marked as AggressiveInlining to handle the common case of a non-surrogate
            // character. The surrogate case is handled in the slower fallback code path.
            char thisChar = *pChar;
            return (Char.IsSurrogate(thisChar)) ? GetScalarValueFromUtf16Slow(pChar, endOfString) : thisChar;
        }

        private static int GetScalarValueFromUtf16Slow(char* pChar, bool endOfString)
        {
            char firstChar = pChar[0];

            if (!Char.IsSurrogate(firstChar))
            {
                Debug.Fail("This case should've been handled by the fast path.");
                return firstChar;
            }
            else if (Char.IsHighSurrogate(firstChar))
            {
                if (endOfString)
                {
                    // unmatched surrogate - substitute
                    return UNICODE_REPLACEMENT_CHAR;
                }
                else
                {
                    char secondChar = pChar[1];
                    if (Char.IsLowSurrogate(secondChar))
                    {
                        // valid surrogate pair - extract codepoint
                        return GetScalarValueFromUtf16SurrogatePair(firstChar, secondChar);
                    }
                    else
                    {
                        // unmatched surrogate - substitute
                        return UNICODE_REPLACEMENT_CHAR;
                    }
                }
            }
            else
            {
                // unmatched surrogate - substitute
                Debug.Assert(Char.IsLowSurrogate(firstChar));
                return UNICODE_REPLACEMENT_CHAR;
            }
        }

        private static int GetScalarValueFromUtf16SurrogatePair(char highSurrogate, char lowSurrogate)
        {
            Debug.Assert(Char.IsHighSurrogate(highSurrogate));
            Debug.Assert(Char.IsLowSurrogate(lowSurrogate));

            // See http://www.unicode.org/versions/Unicode6.2.0/ch03.pdf, Table 3.5 for the
            // details of this conversion. We don't use Char.ConvertToUtf32 because its exception
            // handling shows up on the hot path, and our caller has already sanitized the inputs.
            return (lowSurrogate & 0x3ff) | (((highSurrogate & 0x3ff) + (1 << 6)) << 10);
        }

        internal static void GetUtf16SurrogatePairFromAstralScalarValue(int scalar, out char highSurrogate, out char lowSurrogate)
        {
            Debug.Assert(0x10000 <= scalar && scalar <= UNICODE_LAST_CODEPOINT);

            // See http://www.unicode.org/versions/Unicode6.2.0/ch03.pdf, Table 3.5 for the
            // details of this conversion. We don't use Char.ConvertFromUtf32 because its exception
            // handling shows up on the hot path, it allocates temporary strings (which we don't want),
            // and our caller has already sanitized the inputs.

            int x = scalar & 0xFFFF;
            int u = scalar >> 16;
            int w = u - 1;
            highSurrogate = (char)(0xD800 | (w << 6) | (x >> 10));
            lowSurrogate = (char)(0xDC00 | (x & 0x3FF));
        }

        /// <summary>
        /// Given a Unicode scalar value, returns the UTF-8 representation of the value.
        /// The return value's bytes should be popped from the LSB.
        /// </summary>
        internal static int GetUtf8RepresentationForScalarValue(uint scalar)
        {
            Debug.Assert(scalar <= UNICODE_LAST_CODEPOINT);

            // See http://www.unicode.org/versions/Unicode6.2.0/ch03.pdf, Table 3.6 for the
            // details of this conversion. We don't use UTF8Encoding since we're encoding
            // a scalar code point, not a UTF16 character sequence.
            if (scalar <= 0x7f)
            {
                // one byte used: scalar 00000000 0xxxxxxx -> byte sequence 0xxxxxxx
                byte firstByte = (byte)scalar;
                return firstByte;
            }
            else if (scalar <= 0x7ff)
            {
                // two bytes used: scalar 00000yyy yyxxxxxx -> byte sequence 110yyyyy 10xxxxxx
                byte firstByte = (byte)(0xc0 | (scalar >> 6));
                byte secondByteByte = (byte)(0x80 | (scalar & 0x3f));
                return ((secondByteByte << 8) | firstByte);
            }
            else if (scalar <= 0xffff)
            {
                // three bytes used: scalar zzzzyyyy yyxxxxxx -> byte sequence 1110zzzz 10yyyyyy 10xxxxxx
                byte firstByte = (byte)(0xe0 | (scalar >> 12));
                byte secondByte = (byte)(0x80 | ((scalar >> 6) & 0x3f));
                byte thirdByte = (byte)(0x80 | (scalar & 0x3f));
                return ((((thirdByte << 8) | secondByte) << 8) | firstByte);
            }
            else
            {
                // four bytes used: scalar 000uuuuu zzzzyyyy yyxxxxxx -> byte sequence 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx
                byte firstByte = (byte)(0xf0 | (scalar >> 18));
                byte secondByte = (byte)(0x80 | ((scalar >> 12) & 0x3f));
                byte thirdByte = (byte)(0x80 | ((scalar >> 6) & 0x3f));
                byte fourthByte = (byte)(0x80 | (scalar & 0x3f));
                return ((((((fourthByte << 8) | thirdByte) << 8) | secondByte) << 8) | firstByte);
            }
        }

        /// <summary>
        /// Returns a value stating whether a character is defined per version 8.0
        /// of the Unicode specification. Certain classes of characters (control chars,
        /// private use, surrogates, some whitespace) are considered "undefined" for
        /// our purposes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsCharacterDefined(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            return ((GetDefinedCharacterBitmap()[index] >> offset) & 0x1U) != 0;
        }

        /// <summary>
        /// Determines whether the given scalar value is in the supplementary plane and thus
        /// requires 2 characters to be represented in UTF-16 (as a surrogate pair).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSupplementaryCodePoint(int scalar)
        {
            return ((scalar & ~((int)Char.MaxValue)) != 0);
        }
    }
}
