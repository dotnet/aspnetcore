// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public static class Http2FrameReader
    {
        public static bool ReadFrame(ReadOnlySequence<byte> readableBuffer, Http2Frame frame, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = readableBuffer.Start;
            examined = readableBuffer.End;

            if (readableBuffer.Length < Http2Frame.HeaderLength)
            {
                return false;
            }

            var headerSlice = readableBuffer.Slice(0, Http2Frame.HeaderLength);
            headerSlice.CopyTo(frame.Raw);

            if (readableBuffer.Length < Http2Frame.HeaderLength + frame.Length)
            {
                return false;
            }

            readableBuffer.Slice(Http2Frame.HeaderLength, frame.Length).CopyTo(frame.Payload);
            consumed = examined = readableBuffer.GetPosition(Http2Frame.HeaderLength + frame.Length);

            return true;
        }
    }
}
