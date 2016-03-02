// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class UrlPathDecoder
    {
        /// <summary>
        /// Unescapes the string between given memory iterators in place.
        /// </summary>
        /// <param name="start">The iterator points to the beginning of the sequence.</param>
        /// <param name="end">The iterator points to the byte behind the end of the sequence.</param>
        /// <returns>The iterator points to the byte behind the end of the processed sequence.</returns>
        public static MemoryPoolIterator Unescape(MemoryPoolIterator start, MemoryPoolIterator end)
        {
            // the slot to read the input
            var reader = start;

            // the slot to write the unescaped byte
            var writer = reader;

            while (true)
            {
                if (CompareIterators(ref reader, ref end))
                {
                    return writer;
                }

                if (reader.Peek() == '%')
                {
                    var decodeReader = reader;

                    // If decoding process succeeds, the writer iterator will be moved
                    // to the next write-ready location. On the other hand if the scanned
                    // percent-encodings cannot be interpreted as sequence of UTF-8 octets,
                    // these bytes should be copied to output as is. 
                    // The decodeReader iterator is always moved to the first byte not yet 
                    // be scanned after the process. A failed decoding means the chars
                    // between the reader and decodeReader can be copied to output untouched. 
                    if (!DecodeCore(ref decodeReader, ref writer, end))
                    {
                        Copy(reader, decodeReader, ref writer);
                    }

                    reader = decodeReader;
                }
                else
                {
                    writer.Put((byte)reader.Take());
                }
            }
        }

        /// <summary>
        /// Unescape the percent-encodings
        /// </summary>
        /// <param name="reader">The iterator point to the first % char</param>
        /// <param name="writer">The place to write to</param>
        /// <param name="end">The end of the sequence</param>
        private static bool DecodeCore(ref MemoryPoolIterator reader, ref MemoryPoolIterator writer, MemoryPoolIterator end)
        {
            // preserves the original head. if the percent-encodings cannot be interpreted as sequence of UTF-8 octets,
            // bytes from this till the last scanned one will be copied to the memory pointed by writer.
            var byte1 = UnescapePercentEncoding(ref reader, end);
            if (byte1 == -1)
            {
                return false;
            }

            if (byte1 <= 0x7F)
            {
                // first byte < U+007f, it is a single byte ASCII
                writer.Put((byte)byte1);
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
                if (CompareIterators(ref reader, ref end))
                {
                    return false;
                }

                var nextItr = reader;
                var nextByte = UnescapePercentEncoding(ref nextItr, end);
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

                reader = nextItr;
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
            if (byteCount > 0)
            {
                writer.Put((byte)byte1);
            }
            if (byteCount > 1)
            {
                writer.Put((byte)byte2);
            }
            if (byteCount > 2)
            {
                writer.Put((byte)byte3);
            }
            if (byteCount > 3)
            {
                writer.Put((byte)byte4);
            }

            return true;
        }

        private static void Copy(MemoryPoolIterator head, MemoryPoolIterator tail, ref MemoryPoolIterator writer)
        {
            while (!CompareIterators(ref head, ref tail))
            {
                writer.Put((byte)head.Take());
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
        /// <param name="end">The end of the sequence</param>
        /// <returns>The unescaped byte if success. Otherwise return -1.</returns>
        private static int UnescapePercentEncoding(ref MemoryPoolIterator scan, MemoryPoolIterator end)
        {
            if (scan.Take() != '%')
            {
                return -1;
            }

            var probe = scan;

            int value1 = ReadHex(ref probe, end);
            if (value1 == -1)
            {
                return -1;
            }

            int value2 = ReadHex(ref probe, end);
            if (value2 == -1)
            {
                return -1;
            }

            if (SkipUnescape(value1, value2))
            {
                return -1;
            }

            scan = probe;
            return (value1 << 4) + value2;
        }

        /// <summary>
        /// Read the next char and convert it into hexadecimal value.
        /// 
        /// The <paramref name="scan"/> iterator will be moved to the next
        /// byte no matter no matter whether the operation successes.
        /// </summary>
        /// <param name="scan">The value to read</param>
        /// <param name="end">The end of the sequence</param>
        /// <returns>The hexadecimal value if successes, otherwise -1.</returns>
        private static int ReadHex(ref MemoryPoolIterator scan, MemoryPoolIterator end)
        {
            if (CompareIterators(ref scan, ref end))
            {
                return -1;
            }

            var value = scan.Take();
            var isHead = (((value >= '0') && (value <= '9')) ||
                          ((value >= 'A') && (value <= 'F')) ||
                          ((value >= 'a') && (value <= 'f')));

            if (!isHead)
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

        private static bool SkipUnescape(int value1, int value2)
        {
            // skip %2F
            if (value1 == 2 && value2 == 15)
            {
                return true;
            }

            return false;
        }

        private static bool CompareIterators(ref MemoryPoolIterator lhs, ref MemoryPoolIterator rhs)
        {
            // uses ref parameter to save cost of copying
            return (lhs.Block == rhs.Block) && (lhs.Index == rhs.Index);
        }
    }
}
