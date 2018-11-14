// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public  const int MinAllowedMaxFrameSize = 16 * 1024;
        public const int MaxAllowedMaxFrameSize = 16 * 1024 * 1024 - 1;
        public const int HeaderLength = 9;

        private const int LengthOffset = 0;
        private const int TypeOffset = 3;
        private const int FlagsOffset = 4;
        private const int StreamIdOffset = 5;
        private const int PayloadOffset = 9;

        private readonly byte[] _data = new byte[HeaderLength + MinAllowedMaxFrameSize];

        public Span<byte> Raw => new Span<byte>(_data, 0, HeaderLength + Length);

        public int Length
        {
            get => (_data[LengthOffset] << 16) | (_data[LengthOffset + 1] << 8) | _data[LengthOffset + 2];
            set
            {
                _data[LengthOffset] = (byte)((value & 0x00ff0000) >> 16);
                _data[LengthOffset + 1] = (byte)((value & 0x0000ff00) >> 8);
                _data[LengthOffset + 2] = (byte)(value & 0x000000ff);
            }
        }

        public Http2FrameType Type
        {
            get => (Http2FrameType)_data[TypeOffset];
            set
            {
                _data[TypeOffset] = (byte)value;
            }
        }

        public byte Flags
        {
            get => _data[FlagsOffset];
            set
            {
                _data[FlagsOffset] = (byte)value;
            }
        }

        public int StreamId
        {
            get => (int)((uint)((_data[StreamIdOffset] << 24)
                | (_data[StreamIdOffset + 1] << 16)
                | (_data[StreamIdOffset + 2] << 8)
                | _data[StreamIdOffset + 3]) & 0x7fffffff);
            set
            {
                _data[StreamIdOffset] = (byte)((value & 0xff000000) >> 24);
                _data[StreamIdOffset + 1] = (byte)((value & 0x00ff0000) >> 16);
                _data[StreamIdOffset + 2] = (byte)((value & 0x0000ff00) >> 8);
                _data[StreamIdOffset + 3] = (byte)(value & 0x000000ff);
            }
        }

        public Span<byte> Payload => new Span<byte>(_data, PayloadOffset, Length);
    }
}
