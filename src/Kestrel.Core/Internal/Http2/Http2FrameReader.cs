// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Sequences;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public static class Http2FrameReader
    {
        public static bool ReadFrame(ReadOnlyBuffer readableBuffer, Http2Frame frame, out Position consumed, out Position examined)
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
            consumed = examined = readableBuffer.Move(readableBuffer.Start, Http2Frame.HeaderLength + frame.Length);

            return true;
        }
    }
}
