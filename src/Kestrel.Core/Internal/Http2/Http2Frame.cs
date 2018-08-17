// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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
    public partial class Http2Frame
    {
        public const int MinAllowedMaxFrameSize = 16 * 1024;
        public const int MaxAllowedMaxFrameSize = 16 * 1024 * 1024 - 1;
        public const int HeaderLength = 9;

        private const int LengthOffset = 0;
        private const int TypeOffset = 3;
        private const int FlagsOffset = 4;
        private const int StreamIdOffset = 5;
        private const int PayloadOffset = 9;

        private readonly byte[] _data = new byte[HeaderLength + MinAllowedMaxFrameSize];

        public Span<byte> Raw => new Span<byte>(_data, 0, HeaderLength + PayloadLength);

        public int PayloadLength
        {
            get => (int)Bitshifter.ReadUInt24BigEndian(_data.AsSpan(LengthOffset));
            set => Bitshifter.WriteUInt24BigEndian(_data.AsSpan(LengthOffset), (uint)value);
        }

        public Http2FrameType Type
        {
            get => (Http2FrameType)_data[TypeOffset];
            set => _data[TypeOffset] = (byte)value;
        }

        public byte Flags
        {
            get => _data[FlagsOffset];
            set => _data[FlagsOffset] = value;
        }

        public int StreamId
        {
            get => (int)Bitshifter.ReadUInt31BigEndian(_data.AsSpan(StreamIdOffset));
            set => Bitshifter.WriteUInt31BigEndian(_data.AsSpan(StreamIdOffset), (uint)value);
        }

        public Span<byte> Payload => new Span<byte>(_data, PayloadOffset, PayloadLength);

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
    }
}
