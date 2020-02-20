// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3FrameReader
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
        internal static bool TryReadFrame(ref ReadOnlySequence<byte> readableBuffer, Http3RawFrame frame, uint maxFrameSize, out ReadOnlySequence<byte> framePayload)
        {
            framePayload = ReadOnlySequence<byte>.Empty;
            var consumed = readableBuffer.Start;
            var examined = readableBuffer.Start;

            var type = VariableLengthIntegerHelper.GetInteger(readableBuffer, out consumed, out examined);
            if (type == -1)
            {
                return false;
            }

            var firstLengthBuffer = readableBuffer.Slice(consumed);

            var length = VariableLengthIntegerHelper.GetInteger(firstLengthBuffer, out consumed, out examined);

            // Make sure the whole frame is buffered
            if (length == -1)
            {
                return false;
            }

            var startOfFramePayload = readableBuffer.Slice(consumed);
            if (startOfFramePayload.Length < length)
            {
                return false;
            }

            frame.Length = length;
            frame.Type = (Http3FrameType)type;

            // The remaining payload minus the extra fields
            framePayload = startOfFramePayload.Slice(0, length);
            readableBuffer = readableBuffer.Slice(framePayload.End);

            return true;
        }
    }
}
