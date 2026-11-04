// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Internal;

internal sealed class UrlDecoder
{
    /// <summary>
    /// Unescape a URL path
    /// </summary>
    /// <param name="source">The byte span represents a UTF8 encoding url path.</param>
    /// <param name="destination">The byte span where unescaped url path is copied to.</param>
    /// <param name="isFormEncoding">Whether we are doing form encoding or not.</param>
    /// <returns>The length of the byte sequence of the unescaped url path.</returns>
    public static int DecodeRequestLine(ReadOnlySpan<byte> source, Span<byte> destination, bool isFormEncoding)
    {
        if (destination.Length < source.Length)
        {
            throw new ArgumentException(
                "Length of the destination byte span is less than the source.",
                nameof(destination));
        }

        // This requires the destination span to be larger or equal to source span
        source.CopyTo(destination);
        return DecodeInPlace(destination.Slice(0, source.Length), isFormEncoding);
    }

    /// <summary>
    /// Unescape a URL path in place.
    /// </summary>
    /// <param name="buffer">The byte span represents a UTF8 encoding url path.</param>
    /// <param name="isFormEncoding">Whether we are doing form encoding or not.</param>
    /// <returns>The number of the bytes representing the result.</returns>
    /// <remarks>
    /// The unescape is done in place, which means after decoding the result is the subset of
    /// the input span.
    /// </remarks>
    public static int DecodeInPlace(Span<byte> buffer, bool isFormEncoding)
    {
        // the slot to read the input
        var sourceIndex = 0;

        // the slot to write the unescaped byte
        var destinationIndex = 0;

        while (true)
        {
            if (sourceIndex == buffer.Length)
            {
                break;
            }

            if (buffer[sourceIndex] == '+' && isFormEncoding)
            {
                // Set it to ' ' when we are doing form encoding.
                buffer[sourceIndex] = 0x20;
            }
            else if (buffer[sourceIndex] == '%')
            {
                var decodeIndex = sourceIndex;

                // If decoding process succeeds, the writer iterator will be moved
                // to the next write-ready location. On the other hand if the scanned
                // percent-encodings cannot be interpreted as sequence of UTF-8 octets,
                // these bytes should be copied to output as is.
                // The decodeReader iterator is always moved to the first byte not yet
                // be scanned after the process. A failed decoding means the chars
                // between the reader and decodeReader can be copied to output untouched.
                if (!DecodeCore(ref decodeIndex, ref destinationIndex, buffer, isFormEncoding))
                {
                    Copy(sourceIndex, decodeIndex, ref destinationIndex, buffer);
                }

                sourceIndex = decodeIndex;
            }
            else
            {
                buffer[destinationIndex++] = buffer[sourceIndex++];
            }
        }

        return destinationIndex;
    }

    /// <summary>
    /// Unescape the percent-encodings
    /// </summary>
    /// <param name="sourceIndex">The iterator point to the first % char</param>
    /// <param name="destinationIndex">The place to write to</param>
    /// <param name="buffer">The byte array</param>
    /// <param name="isFormEncoding">Whether we are doing form encodoing</param>
    private static bool DecodeCore(ref int sourceIndex, ref int destinationIndex, Span<byte> buffer, bool isFormEncoding)
    {
        // preserves the original head. if the percent-encodings cannot be interpreted as sequence of UTF-8 octets,
        // bytes from this till the last scanned one will be copied to the memory pointed by writer.
        var byte1 = UnescapePercentEncoding(ref sourceIndex, buffer, isFormEncoding);
        if (byte1 == -1)
        {
            return false;
        }

        if (byte1 == 0)
        {
            throw new InvalidOperationException("The path contains null characters.");
        }

        if (byte1 <= 0x7F)
        {
            // first byte < U+007f, it is a single byte ASCII
            buffer[destinationIndex++] = (byte)byte1;
            return true;
        }

        int byte2 = 0, byte3 = 0, byte4 = 0;

        // anticipate more bytes
        int currentDecodeBits;
        int byteCount;
        int expectValueMin;
        if ((byte1 & 0xE0) == 0xC0)
        {
            // 110x xxxx, expect one more byte
            currentDecodeBits = byte1 & 0x1F;
            byteCount = 2;
            expectValueMin = 0x80;
        }
        else if ((byte1 & 0xF0) == 0xE0)
        {
            // 1110 xxxx, expect two more bytes
            currentDecodeBits = byte1 & 0x0F;
            byteCount = 3;
            expectValueMin = 0x800;
        }
        else if ((byte1 & 0xF8) == 0xF0)
        {
            // 1111 0xxx, expect three more bytes
            currentDecodeBits = byte1 & 0x07;
            byteCount = 4;
            expectValueMin = 0x10000;
        }
        else
        {
            // invalid first byte
            return false;
        }

        var remainingBytes = byteCount - 1;
        while (remainingBytes > 0)
        {
            // read following three chars
            if (sourceIndex == buffer.Length)
            {
                return false;
            }

            var nextSourceIndex = sourceIndex;
            var nextByte = UnescapePercentEncoding(ref nextSourceIndex, buffer, isFormEncoding);
            if (nextByte == -1)
            {
                return false;
            }

            if ((nextByte & 0xC0) != 0x80)
            {
                // the follow up byte is not in form of 10xx xxxx
                return false;
            }

            currentDecodeBits = (currentDecodeBits << 6) | (nextByte & 0x3F);
            remainingBytes--;

            if (remainingBytes == 1 && currentDecodeBits >= 0x360 && currentDecodeBits <= 0x37F)
            {
                // this is going to end up in the range of 0xD800-0xDFFF UTF-16 surrogates that
                // are not allowed in UTF-8;
                return false;
            }

            if (remainingBytes == 2 && currentDecodeBits >= 0x110)
            {
                // this is going to be out of the upper Unicode bound 0x10FFFF.
                return false;
            }

            sourceIndex = nextSourceIndex;
            if (byteCount - remainingBytes == 2)
            {
                byte2 = nextByte;
            }
            else if (byteCount - remainingBytes == 3)
            {
                byte3 = nextByte;
            }
            else if (byteCount - remainingBytes == 4)
            {
                byte4 = nextByte;
            }
        }

        if (currentDecodeBits < expectValueMin)
        {
            // overlong encoding (e.g. using 2 bytes to encode something that only needed 1).
            return false;
        }

        // all bytes are verified, write to the output
        // TODO: measure later to determine if the performance of following logic can be improved
        //       the idea is to combine the bytes into short/int and write to span directly to avoid
        //       range check cost
        if (byteCount > 0)
        {
            buffer[destinationIndex++] = (byte)byte1;
        }
        if (byteCount > 1)
        {
            buffer[destinationIndex++] = (byte)byte2;
        }
        if (byteCount > 2)
        {
            buffer[destinationIndex++] = (byte)byte3;
        }
        if (byteCount > 3)
        {
            buffer[destinationIndex++] = (byte)byte4;
        }

        return true;
    }

    private static void Copy<T>(int begin, int end, ref int writer, Span<T> buffer)
    {
        while (begin != end)
        {
            buffer[writer++] = buffer[begin++];
        }
    }

    /// <summary>
    /// Read the percent-encoding and try unescape it.
    ///
    /// The operation first peek at the character the <paramref name="scan"/>
    /// iterator points at. If it is % the <paramref name="scan"/> is then
    /// moved on to scan the following to characters. If the two following
    /// characters are hexadecimal literals they will be unescaped and the
    /// value will be returned.
    ///
    /// If the first character is not % the <paramref name="scan"/> iterator
    /// will be removed beyond the location of % and -1 will be returned.
    ///
    /// If the following two characters can't be successfully unescaped the
    /// <paramref name="scan"/> iterator will be move behind the % and -1
    /// will be returned.
    /// </summary>
    /// <param name="scan">The value to read</param>
    /// <param name="buffer">The byte array</param>
    /// <param name="isFormEncoding">Whether we are decoding a form or not. Will escape '/' if we are doing form encoding</param>
    /// <returns>The unescaped byte if success. Otherwise return -1.</returns>
    private static int UnescapePercentEncoding(ref int scan, Span<byte> buffer, bool isFormEncoding)
    {
        if (buffer[scan++] != '%')
        {
            return -1;
        }

        var probe = scan;

        var value1 = ReadHex(ref probe, buffer);
        if (value1 == -1)
        {
            return -1;
        }

        var value2 = ReadHex(ref probe, buffer);
        if (value2 == -1)
        {
            return -1;
        }

        if (SkipUnescape(value1, value2, isFormEncoding))
        {
            return -1;
        }

        scan = probe;
        return (value1 << 4) + value2;
    }

    /// <summary>
    /// Read the next char and convert it into hexadecimal value.
    ///
    /// The <paramref name="scan"/> index will be moved to the next
    /// byte no matter whether the operation successes.
    /// </summary>
    /// <param name="scan">The index of the byte in the buffer to read</param>
    /// <param name="buffer">The byte span from which the hex to be read</param>
    /// <returns>The hexadecimal value if successes, otherwise -1.</returns>
    private static int ReadHex(ref int scan, Span<byte> buffer)
    {
        if (scan == buffer.Length)
        {
            return -1;
        }

        var value = buffer[scan++];
        var isHex = ((value >= '0') && (value <= '9')) ||
                     ((value >= 'A') && (value <= 'F')) ||
                     ((value >= 'a') && (value <= 'f'));

        if (!isHex)
        {
            return -1;
        }

        if (value <= '9')
        {
            return value - '0';
        }
        else if (value <= 'F')
        {
            return (value - 'A') + 10;
        }
        else // a - f
        {
            return (value - 'a') + 10;
        }
    }

    private static bool SkipUnescape(int value1, int value2, bool isFormEncoding)
    {
        if (isFormEncoding)
        {
            return false;
        }

        // skip %2F - '/'
        if (value1 == 2 && value2 == 15)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unescape a URL path
    /// </summary>
    /// <param name="source">The escape sequences is expected to be well-formed UTF-8 code units.</param>
    /// <param name="destination">The char span where unescaped url path is copied to.</param>
    /// <returns>The length of the char sequence of the unescaped url path.</returns>
    /// <remarks>
    /// Form Encoding is not supported compared to the <see cref="DecodeRequestLine(ReadOnlySpan{byte}, Span{byte}, bool)" />
    /// for performance gains, as current use-cases does not require it.
    /// </remarks>
    public static int DecodeRequestLine(ReadOnlySpan<char> source, Span<char> destination)
    {
        // This requires the destination span to be larger or equal to source span
        // which is validated by Span<T>.CopyTo.
        source.CopyTo(destination);
        return DecodeInPlace(destination.Slice(0, source.Length));
    }

    /// <summary>
    /// Unescape a URL path in place.
    /// </summary>
    /// <param name="buffer">The escape sequences is expected to be well-formed UTF-8 code units.</param>
    /// <returns>The number of the chars representing the result.</returns>
    /// <remarks>
    /// The unescape is done in place, which means after decoding the result is the subset of
    /// the input span.
    /// Form Encoding is not supported compared to the <see cref="DecodeInPlace(Span{byte}, bool)" />
    /// for performance gains, as current use-cases does not require it.
    /// </remarks>
    public static int DecodeInPlace(Span<char> buffer)
    {
        // Compared to the byte overload implementation, this is a different
        // by using the first occurrence of % as the starting position both
        // for the source and the destination index.
        int position = buffer.IndexOf('%');
        if (position == -1)
        {
            return buffer.Length;
        }

        // the slot to read the input
        var sourceIndex = position;

        // the slot to write the unescaped char
        var destinationIndex = position;

        while (true)
        {
            if (sourceIndex == buffer.Length)
            {
                break;
            }

            if (buffer[sourceIndex] == '%')
            {
                var decodeIndex = sourceIndex;

                // If decoding process succeeds, the writer iterator will be moved
                // to the next write-ready location. On the other hand if the scanned
                // percent-encodings cannot be interpreted as sequence of UTF-8 octets,
                // these chars should be copied to output as is.
                // The decodeReader iterator is always moved to the first char not yet
                // be scanned after the process. A failed decoding means the chars
                // between the reader and decodeReader can be copied to output untouched.
                if (!DecodeCore(ref decodeIndex, ref destinationIndex, buffer))
                {
                    Copy(sourceIndex, decodeIndex, ref destinationIndex, buffer);
                }

                sourceIndex = decodeIndex;
            }
            else
            {
                buffer[destinationIndex++] = buffer[sourceIndex++];
            }
        }

        return destinationIndex;
    }

    /// <summary>
    /// Unescape the percent-encodings
    /// </summary>
    /// <param name="sourceIndex">The iterator point to the first % char</param>
    /// <param name="destinationIndex">The place to write to</param>
    /// <param name="buffer">The char array</param>
    private static bool DecodeCore(ref int sourceIndex, ref int destinationIndex, Span<char> buffer)
    {
        // preserves the original head. if the percent-encodings cannot be interpreted as sequence of UTF-8 octets,
        // chars from this till the last scanned one will be copied to the memory pointed by writer.
        var codeUnit1 = UnescapePercentEncoding(ref sourceIndex, buffer);
        if (codeUnit1 == -1)
        {
            return false;
        }

        if (codeUnit1 == 0)
        {
            throw new InvalidOperationException("The path contains null characters.");
        }

        if (codeUnit1 <= 0x7F)
        {
            // first code unit < U+007f, it is a single char ASCII
            buffer[destinationIndex++] = (char)codeUnit1;
            return true;
        }

        // anticipate more code units
        int currentDecodeBits;
        int codeUnitCount;
        int expectValueMin;
        if ((codeUnit1 & 0xE0) == 0xC0)
        {
            // 110x xxxx, expect one more code unit
            currentDecodeBits = codeUnit1 & 0x1F;
            codeUnitCount = 2;
            expectValueMin = 0x80;
        }
        else if ((codeUnit1 & 0xF0) == 0xE0)
        {
            // 1110 xxxx, expect two more code units
            currentDecodeBits = codeUnit1 & 0x0F;
            codeUnitCount = 3;
            expectValueMin = 0x800;
        }
        else if ((codeUnit1 & 0xF8) == 0xF0)
        {
            // 1111 0xxx, expect three more code units
            currentDecodeBits = codeUnit1 & 0x07;
            codeUnitCount = 4;
            expectValueMin = 0x10000;
        }
        else
        {
            // invalid first code unit
            return false;
        }

        var remainingCodeUnits = codeUnitCount - 1;
        while (remainingCodeUnits > 0)
        {
            // read following three code units
            if (sourceIndex == buffer.Length)
            {
                return false;
            }

            var nextSourceIndex = sourceIndex;
            var nextCodeUnit = UnescapePercentEncoding(ref nextSourceIndex, buffer);
            if (nextCodeUnit == -1)
            {
                return false;
            }

            // When UnescapePercentEncoding returns -1 we shall return false.
            // For performance reasons, there is no separate if statement for the above check
            // as the condition below also returns -1 for that case.
            if ((nextCodeUnit & 0xC0) != 0x80)
            {
                // the follow up code unit is not in form of 10xx xxxx
                return false;
            }

            currentDecodeBits = (currentDecodeBits << 6) | (nextCodeUnit & 0x3F);
            remainingCodeUnits--;

            sourceIndex = nextSourceIndex;
        }

        if (currentDecodeBits < expectValueMin)
        {
            // overlong encoding
            return false;
        }

        if (!System.Text.Rune.TryCreate(currentDecodeBits, out var rune) || !rune.TryEncodeToUtf16(buffer.Slice(destinationIndex), out var charsWritten))
        {
            // Reasons for this failure could be:
            // Value is in the range of 0xD800-0xDFFF UTF-16 surrogates that are not allowed in UTF-8
            // Value is above the upper Unicode bound of 0x10FFFF
            return false;
        }

        destinationIndex += charsWritten;
        return true;
    }

    /// <summary>
    /// Read the percent-encoding and try unescape it.
    ///
    /// The operation first peek at the character the <paramref name="scan"/>
    /// iterator points at. If it is % the <paramref name="scan"/> is then
    /// moved on to scan the following to characters. If the two following
    /// characters are hexadecimal literals they will be unescaped and the
    /// value will be returned.
    ///
    /// If the first character is not % the <paramref name="scan"/> iterator
    /// will be removed beyond the location of % and -1 will be returned.
    ///
    /// If the following two characters can't be successfully unescaped the
    /// <paramref name="scan"/> iterator will be move behind the % and -1
    /// will be returned.
    /// </summary>
    /// <param name="scan">The value to read</param>
    /// <param name="buffer">The char array</param>
    /// <returns>The unescaped char if success. Otherwise return -1.</returns>
    private static int UnescapePercentEncoding(ref int scan, ReadOnlySpan<char> buffer)
    {
        int tempIdx = scan++;
        if (buffer[tempIdx] != '%')
        {
            return -1;
        }

        var probe = scan;

        int firstNibble = ReadHex(ref probe, buffer);
        int secondNibble = ReadHex(ref probe, buffer);
        int value = firstNibble << 4 | secondNibble;

        // Skip invalid hex values and %2F - '/'
        if (value < 0 || value == '/')
        {
            return -1;
        }
        scan = probe;
        return value;
    }

    /// <summary>
    /// Read the next char and convert it into hexadecimal value.
    ///
    /// The <paramref name="scan"/> index will be moved to the next
    /// char no matter whether the operation successes.
    /// </summary>
    /// <param name="scan">The index of the char in the buffer to read</param>
    /// <param name="buffer">The char span from which the hex to be read</param>
    /// <returns>The hexadecimal value if successes, otherwise -1.</returns>
    private static int ReadHex(ref int scan, ReadOnlySpan<char> buffer)
    {
        // To eliminate boundary checks, using a temporary variable tempIdx.
        int tempIdx = scan++;
        if ((uint)tempIdx >= (uint)buffer.Length)
        {
            return -1;
        }
        int value = buffer[tempIdx];

        return FromChar(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FromChar(int c)
    {
        return (uint)c >= (uint)CharToHexLookup.Length ? -1 : CharToHexLookup[c];
    }

    private static ReadOnlySpan<sbyte> CharToHexLookup =>
    [
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 15
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 31
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 47
            0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9,  -1,  -1,  -1,  -1,  -1,  -1, // 63
             -1, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 79
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 95
             -1, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 111
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 127
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 143
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 159
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 175
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 191
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 207
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 223
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1, // 239
             -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1,  -1  // 255
    ];
}
