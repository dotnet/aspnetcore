// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3FrameReader
{
    /* https://quicwg.org/base-drafts/draft-ietf-quic-http.html#frame-layout
         0                   1                   2                   3
         0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |                           Type (i)                          ...
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |                          Length (i)                         ...
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |                       Frame Payload (*)                     ...
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    */
    // Reads and returns partial frames, don't rely on the frame being complete when using this method
    // Set isContinuedFrame to true when expecting to read more of the previous frame
    internal static bool TryReadFrame(ref ReadOnlySequence<byte> readableBuffer, Http3RawFrame frame, bool isContinuedFrame, out ReadOnlySequence<byte> framePayload)
    {
        framePayload = ReadOnlySequence<byte>.Empty;
        SequencePosition consumed = readableBuffer.Start;
        var length = frame.RemainingLength;

        if (!isContinuedFrame)
        {
            if (!VariableLengthIntegerHelper.TryGetInteger(readableBuffer, out consumed, out var type))
            {
                return false;
            }

            var firstLengthBuffer = readableBuffer.Slice(consumed);

            if (!VariableLengthIntegerHelper.TryGetInteger(firstLengthBuffer, out consumed, out length))
            {
                return false;
            }

            frame.RemainingLength = length;
            frame.Type = (Http3FrameType)type;
        }

        var startOfFramePayload = readableBuffer.Slice(consumed);

        // Get all the available bytes or the rest of the frame whichever is less
        length = Math.Min(startOfFramePayload.Length, length);

        // If we were expecting a non-empty payload, but haven't received any of it yet,
        // there is nothing to process until we wait for more data.
        if (length == 0 && frame.RemainingLength != 0)
        {
            return false;
        }

        // The remaining payload minus the extra fields
        framePayload = startOfFramePayload.Slice(0, length);
        readableBuffer = readableBuffer.Slice(framePayload.End);

        return true;
    }
}
