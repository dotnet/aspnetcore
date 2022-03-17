// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace System.Net.Http
{
    /// <summary>
    /// Variable length integer encoding and decoding methods. Based on https://tools.ietf.org/html/draft-ietf-quic-transport-24#section-16.
    /// A variable-length integer can use 1, 2, 4, or 8 bytes.
    /// </summary>
    internal static class VariableLengthIntegerHelper
    {
        public const int MaximumEncodedLength = 8;

        // The high 4 bits indicate the length of the integer.
        // 00 = length 1
        // 01 = length 2
        // 10 = length 4
        // 11 = length 8
        private const byte LengthMask = 0xC0;
        private const byte InitialOneByteLengthMask = 0x00;
        private const byte InitialTwoByteLengthMask = 0x40;
        private const byte InitialFourByteLengthMask = 0x80;
        private const byte InitialEightByteLengthMask = 0xC0;

        // Bits to subtract to remove the length.
        private const uint TwoByteLengthMask = 0x4000;
        private const uint FourByteLengthMask = 0x80000000;
        private const ulong EightByteLengthMask = 0xC000000000000000;

        // public for internal use in aspnetcore
        public const uint OneByteLimit = (1U << 6) - 1;
        public const uint TwoByteLimit = (1U << 14) - 1;
        public const uint FourByteLimit = (1U << 30) - 1;
        public const long EightByteLimit = (1L << 62) - 1;

        public static bool TryRead(ReadOnlySpan<byte> buffer, out long value, out int bytesRead)
        {
            if (buffer.Length != 0)
            {
                byte firstByte = buffer[0];

                switch (firstByte & LengthMask)
                {
                    case InitialOneByteLengthMask:
                        value = firstByte;
                        bytesRead = 1;
                        return true;
                    case InitialTwoByteLengthMask:
                        if (BinaryPrimitives.TryReadUInt16BigEndian(buffer, out ushort serializedShort))
                        {
                            value = serializedShort - TwoByteLengthMask;
                            bytesRead = 2;
                            return true;
                        }
                        break;
                    case InitialFourByteLengthMask:
                        if (BinaryPrimitives.TryReadUInt32BigEndian(buffer, out uint serializedInt))
                        {
                            value = serializedInt - FourByteLengthMask;
                            bytesRead = 4;
                            return true;
                        }
                        break;
                    default: // InitialEightByteLengthMask
                        Debug.Assert((firstByte & LengthMask) == InitialEightByteLengthMask);
                        if (BinaryPrimitives.TryReadUInt64BigEndian(buffer, out ulong serializedLong))
                        {
                            value = (long)(serializedLong - EightByteLengthMask);
                            Debug.Assert(value >= 0 && value <= EightByteLimit, "Serialized values are within [0, 2^62).");

                            bytesRead = 8;
                            return true;
                        }
                        break;
                }
            }

            value = 0;
            bytesRead = 0;
            return false;
        }

        public static bool TryRead(ref SequenceReader<byte> reader, out long value)
        {
            // Hot path: we probably have the entire integer in one unbroken span.
            if (TryRead(reader.UnreadSpan, out value, out int bytesRead))
            {
                reader.Advance(bytesRead);
                return true;
            }

            // Cold path: copy to a temporary buffer before calling span-based read.
            return TryReadSlow(ref reader, out value);

            static bool TryReadSlow(ref SequenceReader<byte> reader, out long value)
            {
                ReadOnlySpan<byte> span = reader.CurrentSpan;

                if (reader.TryPeek(out byte firstByte))
                {
                    int length =
                        (firstByte & LengthMask) switch
                        {
                            InitialOneByteLengthMask => 1,
                            InitialTwoByteLengthMask => 2,
                            InitialFourByteLengthMask => 4,
                            _ => 8 // LengthEightByte
                        };

                    Span<byte> temp = (stackalloc byte[8])[..length];
                    if (reader.TryCopyTo(temp))
                    {
                        bool result = TryRead(temp, out value, out int bytesRead);
                        Debug.Assert(result == true);
                        Debug.Assert(bytesRead == length);

                        reader.Advance(bytesRead);
                        return true;
                    }
                }

                value = 0;
                return false;
            }
        }

        public static long GetInteger(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            var reader = new SequenceReader<byte>(buffer);
            if (TryRead(ref reader, out long value))
            {
                consumed = examined = buffer.GetPosition(reader.Consumed);
                return value;
            }
            else
            {
                consumed = default;
                examined = buffer.End;
                return -1;
            }
        }

        public static bool TryWrite(Span<byte> buffer, long longToEncode, out int bytesWritten)
        {
            Debug.Assert(longToEncode >= 0);
            Debug.Assert(longToEncode <= EightByteLimit);

            if (longToEncode <= OneByteLimit)
            {
                if (buffer.Length != 0)
                {
                    buffer[0] = (byte)longToEncode;
                    bytesWritten = 1;
                    return true;
                }
            }
            else if (longToEncode <= TwoByteLimit)
            {
                if (BinaryPrimitives.TryWriteUInt16BigEndian(buffer, (ushort)((uint)longToEncode | TwoByteLengthMask)))
                {
                    bytesWritten = 2;
                    return true;
                }
            }
            else if (longToEncode <= FourByteLimit)
            {
                if (BinaryPrimitives.TryWriteUInt32BigEndian(buffer, (uint)longToEncode | FourByteLengthMask))
                {
                    bytesWritten = 4;
                    return true;
                }
            }
            else // EightByteLimit
            {
                if (BinaryPrimitives.TryWriteUInt64BigEndian(buffer, (ulong)longToEncode | EightByteLengthMask))
                {
                    bytesWritten = 8;
                    return true;
                }
            }

            bytesWritten = 0;
            return false;
        }

        public static int WriteInteger(Span<byte> buffer, long longToEncode)
        {
            bool res = TryWrite(buffer, longToEncode, out int bytesWritten);
            Debug.Assert(res == true);
            return bytesWritten;
        }

        public static int GetByteCount(long value)
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= EightByteLimit);

            return
                value <= OneByteLimit ? 1 :
                value <= TwoByteLimit ? 2 :
                value <= FourByteLimit ? 4 :
                8; // EightByteLimit
        }
    }
}
