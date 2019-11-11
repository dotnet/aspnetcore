// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    /// <summary>
    /// Variable length integer encoding and decoding methods. Based on https://tools.ietf.org/html/draft-ietf-quic-transport-24#section-16.
    /// Either will take up 1, 2, 4, or 8 bytes.
    /// </summary>
    internal static class VariableLengthIntegerHelper
    {
        private const int TwoByteSubtract = 0x4000;
        private const uint FourByteSubtract = 0x80000000;
        private const ulong EightByteSubtract = 0xC000000000000000;
        private const int OneByteLimit = 64;
        private const int TwoByteLimit = 16383;
        private const int FourByteLimit = 1073741823;

        // Per the HTTP/3 spec, the following variable length integer values aren't allowed
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#frame-reserved
        // TODO actually use this method to block streamIds.
        public static long GetVariableIntErrorIfNotAllowedValue(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
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

            // The first two bits of the first byte represent the length of the
            // variable length integer
            // 00 = length 1
            // 01 = length 2
            // 10 = length 4
            // 11 = length 8

            var span = buffer.Slice(0, Math.Min(buffer.Length, 8)).ToSpan();

            var firstByte = span[0];

            if ((firstByte & 0xC0) == 0)
            {
                consumed = examined = buffer.Slice(1).Start;
                return firstByte & 0x3F;
            }
            else if ((firstByte & 0xC0) == 0x40)
            {
                if (buffer.Length < 2)
                {
                    return -1;
                }

                consumed = examined = buffer.Slice(0, 2).End;

                return BinaryPrimitives.ReadUInt16BigEndian(span) - TwoByteSubtract;
            }
            else if ((firstByte & 0xC0) == 0x80)
            {
                if (buffer.Length < 4)
                {
                    return -1;
                }
                consumed = examined = buffer.Slice(0, 4).End;

                return BinaryPrimitives.ReadUInt32BigEndian(span) - FourByteSubtract;
            }
            else
            {
                if (buffer.Length < 8)
                {
                    return -1;
                }

                consumed = examined = buffer.Slice(0, 8).End;

                return (long)(BinaryPrimitives.ReadUInt64BigEndian(span) - EightByteSubtract);
            }
        }

        public static int WriteEncodedIntegerToMemory(Memory<byte> buffer, long longToEncode)
        {
            return WriteEncodedIntegerToSpan(buffer.Span, longToEncode);
        }

        public static int WriteEncodedIntegerToSpan(Span<byte> buffer, long longToEncode)
        {
            Debug.Assert(buffer.Length >= 8);
            Debug.Assert(longToEncode < long.MaxValue / 2);

            if (longToEncode < OneByteLimit)
            {
                buffer[0] = (byte)longToEncode;
                return 1;
            }
            else if (longToEncode < TwoByteLimit)
            {
                buffer[0] = (byte)((longToEncode >> 8) + 0x40);
                buffer[1] = (byte)((longToEncode & 0xFF));
                return 2;
            }
            else if (longToEncode < FourByteLimit)
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
                buffer[2] = (byte)((longToEncode >> 40) & 0xFF);
                buffer[3] = (byte)((longToEncode >> 32) & 0xFF);
                buffer[4] = (byte)((longToEncode >> 24) & 0xFF);
                buffer[5] = (byte)((longToEncode >> 16) & 0xFF);
                buffer[6] = (byte)((longToEncode >> 8) & 0xFF);
                buffer[7] = (byte)((longToEncode) & 0xFF);
                return 8;
            }
        }
    }
}
