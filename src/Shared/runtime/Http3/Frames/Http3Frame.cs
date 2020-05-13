// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http
{
    internal static partial class Http3Frame
    {
        public const int MaximumEncodedFrameEnvelopeLength = 1 + VariableLengthIntegerHelper.MaximumEncodedLength; // Frame type + payload length.

        /// <summary>
        /// Reads two variable-length integers.
        /// </summary>
        public static bool TryReadIntegerPair(ReadOnlySpan<byte> buffer, out long a, out long b, out int bytesRead)
        {
            if (VariableLengthIntegerHelper.TryRead(buffer, out a, out int aLength))
            {
                buffer = buffer.Slice(aLength);
                if (VariableLengthIntegerHelper.TryRead(buffer, out b, out int bLength))
                {
                    bytesRead = aLength + bLength;
                    return true;
                }
            }

            b = 0;
            bytesRead = 0;
            return false;
        }

        //  0                   1                   2                   3
        //  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                           Type (i)                          ...
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                          Length (i)                         ...
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                       Frame Payload (*)                     ...
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        public static bool TryWriteFrameEnvelope(Http3FrameType frameType, long payloadLength, Span<byte> buffer, out int bytesWritten)
        {
            Debug.Assert(VariableLengthIntegerHelper.GetByteCount((long)frameType) == 1, $"{nameof(TryWriteFrameEnvelope)} assumes {nameof(frameType)} will fit within a single byte varint.");

            if (buffer.Length != 0)
            {
                buffer[0] = (byte)frameType;
                buffer = buffer.Slice(1);

                if (VariableLengthIntegerHelper.TryWrite(buffer, payloadLength, out int payloadLengthEncodedLength))
                {
                    bytesWritten = payloadLengthEncodedLength + 1;
                    return true;
                }
            }

            bytesWritten = 0;
            return false;
        }
    }
}
