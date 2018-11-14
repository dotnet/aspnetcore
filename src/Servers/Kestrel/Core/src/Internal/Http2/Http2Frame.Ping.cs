// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public Http2PingFrameFlags PingFlags
        {
            get => (Http2PingFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public void PreparePing(Http2PingFrameFlags flags)
        {
            Length = 8;
            Type = Http2FrameType.PING;
            PingFlags = flags;
            StreamId = 0;
        }
    }
}
