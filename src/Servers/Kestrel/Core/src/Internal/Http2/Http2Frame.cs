// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    /* https://tools.ietf.org/html/rfc7540#section-4.1
        +-----------------------------------------------+
        |                 Length (24)                   |
        +---------------+---------------+---------------+
        |   Type (8)    |   Flags (8)   |
        +-+-------------+---------------+-------------------------------+
        |R|                 Stream Identifier (31)                      |
        +=+=============================================================+
        |                   Frame Payload (0...)                      ...
        +---------------------------------------------------------------+
    */
    internal partial class Http2Frame
    {
        public int PayloadLength { get; set; }

        public Http2FrameType Type { get; set; }

        public byte Flags { get; set; }

        public int StreamId { get; set; }

        internal object ShowFlags()
        {
            switch (Type)
            {
                case Http2FrameType.CONTINUATION:
                    return ContinuationFlags;
                case Http2FrameType.DATA:
                    return DataFlags;
                case Http2FrameType.HEADERS:
                    return HeadersFlags;
                case Http2FrameType.SETTINGS:
                    return SettingsFlags;
                case Http2FrameType.PING:
                    return PingFlags;

                // Not Implemented
                case Http2FrameType.PUSH_PROMISE:

                // No flags defined
                case Http2FrameType.PRIORITY:
                case Http2FrameType.RST_STREAM:
                case Http2FrameType.GOAWAY:
                case Http2FrameType.WINDOW_UPDATE:
                default:
                    return $"0x{Flags:x}";
            }
        }

        public override string ToString()
        {
            return $"{Type} Stream: {StreamId} Length: {PayloadLength} Flags: {ShowFlags()}";
        }
    }
}
