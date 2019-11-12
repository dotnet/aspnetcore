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
                consumed = examined = buffer.GetPosition(1);
                return firstByte & 0x3F;
            }
            else if ((firstByte & 0xC0) == 0x40)
            {
                if (span.Length < 2)
                {
                    return -1;
                }

                consumed = examined = buffer.GetPosition(2);

                return BinaryPrimitives.ReadUInt16BigEndian(span) - TwoByteSubtract;
            }
            else if ((firstByte & 0xC0) == 0x80)
            {
                if (span.Length < 4)
                {
                    return -1;
                }

                consumed = examined = buffer.GetPosition(4);

                return BinaryPrimitives.ReadUInt32BigEndian(span) - FourByteSubtract;
            }
            else
            {
                if (span.Length < 8)
                {
                    return -1;
                }

                consumed = examined = buffer.GetPosition(8);

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
                BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)longToEncode);
                buffer[0] += 0x40;
                return 2;
            }
            else if (longToEncode < FourByteLimit)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)longToEncode);
                buffer[0] += 0x80;
                return 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(buffer, (ulong)longToEncode);
                buffer[0] += 0xC0;
                return 8;
            }
        }
    }
}
