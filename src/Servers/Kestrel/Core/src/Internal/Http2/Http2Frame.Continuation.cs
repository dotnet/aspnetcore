// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public Http2ContinuationFrameFlags ContinuationFlags
        {
            get => (Http2ContinuationFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public void PrepareContinuation(Http2ContinuationFrameFlags flags, int streamId)
        {
            Length = MinAllowedMaxFrameSize - HeaderLength;
            Type = Http2FrameType.CONTINUATION;
            ContinuationFlags = flags;
            StreamId = streamId;
        }
    }
}
