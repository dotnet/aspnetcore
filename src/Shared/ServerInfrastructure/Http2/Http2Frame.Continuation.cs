// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    /* https://tools.ietf.org/html/rfc7540#section-6.10
        +---------------------------------------------------------------+
        |                   Header Block Fragment (*)                 ...
        +---------------------------------------------------------------+
    */
    internal partial class Http2Frame
    {
        public Http2ContinuationFrameFlags ContinuationFlags
        {
            get => (Http2ContinuationFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public bool ContinuationEndHeaders => (ContinuationFlags & Http2ContinuationFrameFlags.END_HEADERS) == Http2ContinuationFrameFlags.END_HEADERS;

        public void PrepareContinuation(Http2ContinuationFrameFlags flags, int streamId)
        {
            PayloadLength = 0;
            Type = Http2FrameType.CONTINUATION;
            ContinuationFlags = flags;
            StreamId = streamId;
        }
    }
}
