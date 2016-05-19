// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    class Streams
    {
        public Streams(IFrameControl frameControl)
        {
            RequestBody = new FrameRequestStream();
            ResponseBody = new FrameResponseStream(frameControl);
            DuplexStream = new FrameDuplexStream(RequestBody, ResponseBody);
        }

        public FrameRequestStream RequestBody { get; }
        public FrameResponseStream ResponseBody { get; }
        public FrameDuplexStream DuplexStream { get; }
    }
}
