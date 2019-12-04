// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    /* https://tools.ietf.org/html/rfc7540#section-6.2
        +---------------+
        |Pad Length? (8)|
        +-+-------------+-----------------------------------------------+
        |E|                 Stream Dependency? (31)                     |
        +-+-------------+-----------------------------------------------+
        |  Weight? (8)  |
        +-+-------------+-----------------------------------------------+
        |                   Header Block Fragment (*)                 ...
        +---------------------------------------------------------------+
        |                           Padding (*)                       ...
        +---------------------------------------------------------------+
    */
    internal partial class Http2Frame
    {
        public Http2HeadersFrameFlags HeadersFlags
        {
            get => (Http2HeadersFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public bool HeadersEndHeaders => (HeadersFlags & Http2HeadersFrameFlags.END_HEADERS) == Http2HeadersFrameFlags.END_HEADERS;

        public bool HeadersEndStream => (HeadersFlags & Http2HeadersFrameFlags.END_STREAM) == Http2HeadersFrameFlags.END_STREAM;

        public bool HeadersHasPadding => (HeadersFlags & Http2HeadersFrameFlags.PADDED) == Http2HeadersFrameFlags.PADDED;

        public bool HeadersHasPriority => (HeadersFlags & Http2HeadersFrameFlags.PRIORITY) == Http2HeadersFrameFlags.PRIORITY;

        public byte HeadersPadLength { get; set; }

        public int HeadersStreamDependency { get; set; }

        public byte HeadersPriorityWeight { get; set; }

        private int HeadersPayloadOffset => (HeadersHasPadding ? 1 : 0) + (HeadersHasPriority ? 5 : 0);

        public int HeadersPayloadLength => PayloadLength - HeadersPayloadOffset - HeadersPadLength;

        public void PrepareHeaders(Http2HeadersFrameFlags flags, int streamId)
        {
            PayloadLength = 0;
            Type = Http2FrameType.HEADERS;
            HeadersFlags = flags;
            StreamId = streamId;
        }
    }
}
