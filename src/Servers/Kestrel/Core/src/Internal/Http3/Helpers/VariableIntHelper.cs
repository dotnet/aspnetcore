// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    public static class VariableIntHelper
    {
        public static long GetVariableIntErrorIfNotAllowedValue(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            // 0x1F * N + 0x21 isn't allowed by spec, return -1 if that's the case.
            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#frame-reserved
            var longLength = GetVariableIntFromReadOnlySequence(buffer, out consumed, out examined);
            if ((longLength - 0x21) % 0x1F == 0)
            {
                return -1;
            }

            return longLength;
        }

        public static long GetVariableIntFromReadOnlySequence(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;

            if (buffer.Length == 0)
            {
                return -1;
            }

            var ros = buffer.Slice(0, 1);
            var firstByte = ros.FirstSpan[0];
            // TODO don't use first span here.
            if ((firstByte & 0xC0) == 0)
            {
                consumed = examined = ros.End;
                return firstByte & 0x3F;
            }
            else if ((firstByte & 0xC0) == 0x40)
            {
                if (buffer.Length < 2)
                {
                    return -1;
                }
                var twoByte = buffer.Slice(1, 1);
                var twoByteSpan = twoByte.FirstSpan;
                consumed = examined = twoByte.End;
                // TODO confirm bitshifting stuff
                return (long)(firstByte & 0x3F) << 8 | twoByteSpan[0];
            }
            else if ((firstByte & 0xC0) == 0x80)
            {
                if (buffer.Length < 4)
                {
                    return -1;
                }
                var fourByte = buffer.Slice(1, 3);
                var fourByteSpan = fourByte.FirstSpan;
                consumed = examined = fourByte.End;
                return (firstByte & 0x3F) << 24 | fourByteSpan[0] << 16 | fourByteSpan[1] << 8 | fourByteSpan[2];
            }
            else if ((firstByte & 0xC0) == 0xC0)
            {
                if (buffer.Length < 8)
                {
                    return -1;
                }

                var eightByte = buffer.Slice(1, 7);
                var eightByteSpan = eightByte.FirstSpan;
                consumed = examined = eightByte.End;
                ulong result = (ulong)(firstByte & 0x3F) << 56;
                result |= (ulong)eightByteSpan[0] << 48;
                result |= (ulong)eightByteSpan[1] << 40;
                result |= (ulong)eightByteSpan[2] << 32;
                result |= (ulong)eightByteSpan[3] << 24;
                result |= (ulong)eightByteSpan[4] << 16;
                result |= (ulong)eightByteSpan[5] << 8;
                result |= (ulong)eightByteSpan[6];

                // no truncation because max value is less than a long or ulong
                return (long)result;
            }
            else
            {
                throw new Exception("Should not happen");
            }
        }

        public static int WriteEncodedIntegerToMemory(Memory<byte> buffer, long longToEncode)
        {
            return WriteEncodedIntegerToSpan(buffer.Span, longToEncode);
        }

        public static int WriteEncodedIntegerToSpan(Span<byte> buffer, long longToEncode)
        {
            // TODO we should maybe make these longs eventually.
            Debug.Assert(buffer.Length >= 8);
            Debug.Assert(longToEncode < long.MaxValue / 2);

            if (longToEncode < 64)
            {
                buffer[0] = (byte)longToEncode;
                return 1;
            }
            else if (longToEncode < 16383)
            {
                buffer[0] = (byte)((longToEncode >> 8) + 0x40);
                buffer[1] = (byte)((longToEncode & 0xFF));
                return 2;
            }
            else if (longToEncode < 1073741823)
            {
                buffer[0] = (byte)((longToEncode >> 24) + 0x80);
                buffer[1] = (byte)((longToEncode >> 16) & 0xFF);
                buffer[2] = (byte)((longToEncode >> 8) & 0xFF);
                buffer[3] = (byte)((longToEncode) & 0xFF);
                return 4;
            }
            else
            {
                buffer[0] = (byte)((longToEncode >> 56) + 0xC0);
                buffer[1] = (byte)((longToEncode >> 48) & 0xFF);
                buffer[1] = (byte)((longToEncode >> 40) & 0xFF);
                buffer[1] = (byte)((longToEncode >> 32) & 0xFF);
                buffer[1] = (byte)((longToEncode >> 24) & 0xFF);
                buffer[1] = (byte)((longToEncode >> 16) & 0xFF);
                buffer[1] = (byte)((longToEncode >> 8) & 0xFF);
                buffer[1] = (byte)((longToEncode) & 0xFF);
                return 8;
            }
        }
    }
}
