// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public Http2ErrorCode RstStreamErrorCode
        {
            get => (Http2ErrorCode)((Payload[0] << 24) | (Payload[1] << 16) | (Payload[2] << 16) | Payload[3]);
            set
            {
                Payload[0] = (byte)(((uint)value >> 24) & 0xff);
                Payload[1] = (byte)(((uint)value >> 16) & 0xff);
                Payload[2] = (byte)(((uint)value >> 8) & 0xff);
                Payload[3] = (byte)((uint)value & 0xff);
            }
        }

        public void PrepareRstStream(int streamId, Http2ErrorCode errorCode)
        {
            Length = 4;
            Type = Http2FrameType.RST_STREAM;
            Flags = 0;
            StreamId = streamId;
            RstStreamErrorCode = errorCode;
        }
    }
}
