// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public Http2HeadersFrameFlags HeadersFlags
        {
            get => (Http2HeadersFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public bool HeadersHasPadding => (HeadersFlags & Http2HeadersFrameFlags.PADDED) == Http2HeadersFrameFlags.PADDED;

        public byte HeadersPadLength
        {
            get => HeadersHasPadding ? _data[HeaderLength] : (byte)0;
            set => _data[HeaderLength] = value;
        }

        public bool HeadersHasPriority => (HeadersFlags & Http2HeadersFrameFlags.PRIORITY) == Http2HeadersFrameFlags.PRIORITY;

        public byte HeadersPriority
        {
            get => _data[HeadersPriorityOffset];
            set => _data[HeadersPriorityOffset] = value;
        }

        private int HeadersPriorityOffset => PayloadOffset + (HeadersHasPadding ? 1 : 0) + 4;

        public int HeadersStreamDependency
        {
            get
            {
                var offset = HeadersStreamDependencyOffset;

                return (int)((uint)((_data[offset] << 24)
                    | (_data[offset + 1] << 16)
                    | (_data[offset + 2] << 8)
                    | _data[offset + 3]) & 0x7fffffff);
            }
            set
            {
                var offset = HeadersStreamDependencyOffset;

                _data[offset] = (byte)((value & 0xff000000) >> 24);
                _data[offset + 1] = (byte)((value & 0x00ff0000) >> 16);
                _data[offset + 2] = (byte)((value & 0x0000ff00) >> 8);
                _data[offset + 3] = (byte)(value & 0x000000ff);
            }
        }

        private int HeadersStreamDependencyOffset => PayloadOffset + (HeadersHasPadding ? 1 : 0);

        public Span<byte> HeadersPayload => new Span<byte>(_data, HeadersPayloadOffset, HeadersPayloadLength);

        private int HeadersPayloadOffset => PayloadOffset + (HeadersHasPadding ? 1 : 0) + (HeadersHasPriority ? 5 : 0);

        private int HeadersPayloadLength => Length - ((HeadersHasPadding ? 1 : 0) + (HeadersHasPriority ? 5 : 0)) - HeadersPadLength;

        public void PrepareHeaders(Http2HeadersFrameFlags flags, int streamId)
        {
            Length = MinAllowedMaxFrameSize - HeaderLength;
            Type = Http2FrameType.HEADERS;
            HeadersFlags = flags;
            StreamId = streamId;
        }
    }
}
