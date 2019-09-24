// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    // We don't use the cooked URL because http.sys unescapes all percent-encoded values. However,
    // we also can't just use the raw Uri, since http.sys supports not only UTF-8, but also ANSI/DBCS and
    // Unicode code points. System.Uri only supports UTF-8.
    // The purpose of this class is to decode all UTF-8 percent encoded characters, with the
    // exception of %2F ('/'), which is left encoded
    internal static class RequestUriBuilder
    {
        private static readonly Encoding UTF8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        public static string DecodeAndUnescapePath(Span<byte> rawUrlBytes)
        {
            Debug.Assert(rawUrlBytes.Length != 0, "Length of the URL cannot be zero.");
            var rawPath = RawUrlHelper.GetPath(rawUrlBytes);

            if (rawPath.Length == 0)
            {
                return "/";
            }

            // OPTIONS *
            // RemoveDotSegments Asserts path always starts with a '/'
            if (rawPath.Length == 1 && rawPath[0] == (byte)'*')
            {
                return "*";
            }

            var unescapedPath = Unescape(rawPath);

            var length = PathNormalizer.RemoveDotSegments(unescapedPath);

            return UTF8.GetString(unescapedPath.Slice(0, length));
        }

        /// <summary>
        /// Unescape a given path string in place. The given path string  may contain escaped char.
        /// </summary>
        /// <param name="rawPath">The raw path string to be unescaped</param>
        /// <returns>The unescaped path string</returns>
        private static Span<byte> Unescape(Span<byte> rawPath)
        {
            // the slot to read the input
            var reader = 0;

            // the slot to write the unescaped byte
            var writer = 0;

            // the end of the path
            var end = rawPath.Length;

            while (true)
            {
                if (reader == end)
                {
                    break;
                }

                if (rawPath[reader] == '%')
                {
                    var decodeReader = reader;

                    // If decoding process succeeds, the writer iterator will be moved
                    // to the next write-ready location. On the other hand if the scanned
                    // percent-encodings cannot be interpreted as sequence of UTF-8 octets,
                    // these bytes should be copied to output as is.
                    // The decodeReader iterator is always moved to the first byte not yet
                    // be scanned after the process. A failed decoding means the chars
                    // between the reader and decodeReader can be copied to output untouched.
                    if (!DecodeCore(ref decodeReader, ref writer, end, rawPath))
                    {
                        Copy(reader, decodeReader, ref writer, rawPath);
                    }

                    reader = decodeReader;
                }
                else
                {
                    rawPath[writer++] = rawPath[reader++];
                }
            }

            return rawPath.Slice(0, writer);
        }

        /// <summary>
        /// Unescape the percent-encodings
        /// </summary>
        /// <param name="reader">The iterator point to the first % char</param>
        /// <param name="writer">The place to write to</param>
        /// <param name="end">The end of the buffer</param>
        /// <param name="buffer">The byte array</param>
        private static bool DecodeCore(ref int reader, ref int writer, int end, Span<byte> buffer)
        {
            // preserves the original head. if the percent-encodings cannot be interpreted as sequence of UTF-8 octets,
            // bytes from this till the last scanned one will be copied to the memory pointed by writer.
            var byte1 = UnescapePercentEncoding(ref reader, end, buffer);

            if (!byte1.HasValue)
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
                buffer[writer++] = (byte)byte1;
                return true;
            }

            int byte2 = 0, byte3 = 0, byte4 = 0;

            // anticipate more bytes
            var currentDecodeBits = 0;
            var byteCount = 1;
            var expectValueMin = 0;
            if ((byte1 & 0xE0) == 0xC0)
            {
                // 110x xxxx, expect one more byte
                currentDecodeBits = byte1.Value & 0x1F;
                byteCount = 2;
                expectValueMin = 0x80;
            }
            else if ((byte1 & 0xF0) == 0xE0)
            {
                // 1110 xxxx, expect two more bytes
                currentDecodeBits = byte1.Value & 0x0F;
                byteCount = 3;
                expectValueMin = 0x800;
            }
            else if ((byte1 & 0xF8) == 0xF0)
            {
                // 1111 0xxx, expect three more bytes
                currentDecodeBits = byte1.Value & 0x07;
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
                if (reader == buffer.Length)
                {
                    return false;
                }

                var nextItr = reader;
                var nextByte = UnescapePercentEncoding(ref nextItr, end, buffer);
                if (!nextByte.HasValue)
                {
                    return false;
                }

                if ((nextByte & 0xC0) != 0x80)
                {
                    // the follow up byte is not in form of 10xx xxxx
                    return false;
                }

                currentDecodeBits = (currentDecodeBits << 6) | (nextByte.Value & 0x3F);
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

                reader = nextItr;
                if (byteCount - remainingBytes == 2)
                {
                    byte2 = nextByte.Value;
                }
                else if (byteCount - remainingBytes == 3)
                {
                    byte3 = nextByte.Value;
                }
                else if (byteCount - remainingBytes == 4)
                {
                    byte4 = nextByte.Value;
                }
            }

            if (currentDecodeBits < expectValueMin)
            {
                // overlong encoding (e.g. using 2 bytes to encode something that only needed 1).
                return false;
            }

            // all bytes are verified, write to the output
            if (byteCount > 0)
            {
                buffer[writer++] = (byte)byte1;
            }
            if (byteCount > 1)
            {
                buffer[writer++] = (byte)byte2;
            }
            if (byteCount > 2)
            {
                buffer[writer++] = (byte)byte3;
            }
            if (byteCount > 3)
            {
                buffer[writer++] = (byte)byte4;
            }

            return true;
        }

        private static void Copy(int begin, int end, ref int writer, Span<byte> buffer)
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
        /// <param name="end">The end of the buffer</param>
        /// <param name="buffer">The byte array</param>
        /// <returns>The unescaped byte if success. Otherwise return -1.</returns>
        private static int? UnescapePercentEncoding(ref int scan, int end, ReadOnlySpan<byte> buffer)
        {
            if (buffer[scan++] != '%')
            {
                return -1;
            }

            var probe = scan;

            var value1 = ReadHex(ref probe, end, buffer);
            if (!value1.HasValue)
            {
                return null;
            }

            var value2 = ReadHex(ref probe, end, buffer);
            if (!value2.HasValue)
            {
                return null;
            }

            if (SkipUnescape(value1.Value, value2.Value))
            {
                return null;
            }

            scan = probe;
            return (value1.Value << 4) + value2.Value;
        }

        /// <summary>
        /// Read the next char and convert it into hexadecimal value.
        ///
        /// The <paramref name="scan"/> iterator will be moved to the next
        /// byte no matter no matter whether the operation successes.
        /// </summary>
        /// <param name="scan">The value to read</param>
        /// <param name="end">The end of the buffer</param>
        /// <param name="buffer">The byte array</param>
        /// <returns>The hexadecimal value if successes, otherwise -1.</returns>
        private static int? ReadHex(ref int scan, int end, ReadOnlySpan<byte> buffer)
        {
            if (scan == end)
            {
                return null;
            }

            var value = buffer[scan++];
            var isHead = (((value >= '0') && (value <= '9')) ||
                          ((value >= 'A') && (value <= 'F')) ||
                          ((value >= 'a') && (value <= 'f')));

            if (!isHead)
            {
                return null;
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

        private static bool SkipUnescape(int value1, int value2)
        {
            // skip %2F - '/'
            if (value1 == 2 && value2 == 15)
            {
                return true;
            }

            return false;
        }
    }
}
