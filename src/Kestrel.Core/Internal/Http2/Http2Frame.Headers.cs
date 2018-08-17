// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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
    public partial class Http2Frame
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

        public byte HeadersPadLength
        {
            get => HeadersHasPadding ? Payload[0] : (byte)0;
            set => Payload[0] = value;
        }

        private int HeadersStreamDependencyOffset => HeadersHasPadding ? 1 : 0;

        public int HeadersStreamDependency
        {
            get => (int)Bitshifter.ReadUInt31BigEndian(Payload.Slice(HeadersStreamDependencyOffset));
            set => Bitshifter.WriteUInt31BigEndian(Payload.Slice(HeadersStreamDependencyOffset), (uint)value);
        }

        private int HeadersPriorityWeightOffset => HeadersStreamDependencyOffset + 4;

        public byte HeadersPriorityWeight
        {
            get => Payload[HeadersPriorityWeightOffset];
            set => Payload[HeadersPriorityWeightOffset] = value;
        }

        public int HeadersPayloadOffset => (HeadersHasPadding ? 1 : 0) + (HeadersHasPriority ? 5 : 0);

        private int HeadersPayloadLength => PayloadLength - HeadersPayloadOffset - HeadersPadLength;

        public Span<byte> HeadersPayload => Payload.Slice(HeadersPayloadOffset, HeadersPayloadLength);

        public void PrepareHeaders(Http2HeadersFrameFlags flags, int streamId)
        {
            PayloadLength = MinAllowedMaxFrameSize - HeaderLength;
            Type = Http2FrameType.HEADERS;
            HeadersFlags = flags;
            StreamId = streamId;
        }
    }
}
