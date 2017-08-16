// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public int GoAwayLastStreamId
        {
            get => (Payload[0] << 24) | (Payload[1] << 16) | (Payload[2] << 16) | Payload[3];
            set
            {
                Payload[0] = (byte)((value >> 24) & 0xff);
                Payload[1] = (byte)((value >> 16) & 0xff);
                Payload[2] = (byte)((value >> 8) & 0xff);
                Payload[3] = (byte)(value & 0xff);
            }
        }

        public Http2ErrorCode GoAwayErrorCode
        {
            get => (Http2ErrorCode)((Payload[4] << 24) | (Payload[5] << 16) | (Payload[6] << 16) | Payload[7]);
            set
            {
                Payload[4] = (byte)(((uint)value >> 24) & 0xff);
                Payload[5] = (byte)(((uint)value >> 16) & 0xff);
                Payload[6] = (byte)(((uint)value >> 8) & 0xff);
                Payload[7] = (byte)((uint)value & 0xff);
            }
        }

        public void PrepareGoAway(int lastStreamId, Http2ErrorCode errorCode)
        {
            Length = 8;
            Type = Http2FrameType.GOAWAY;
            Flags = 0;
            StreamId = 0;
            GoAwayLastStreamId = lastStreamId;
            GoAwayErrorCode = errorCode;
        }
    }
}
