// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    interface IHttpComponentFactory
    {
        KestrelServerOptions ServerOptions { get; set; }

        Streams CreateStreams(FrameContext owner);

        void DisposeStreams(Streams streams);

        Headers CreateHeaders(DateHeaderValueManager dateValueManager);

        void DisposeHeaders(Headers headers);
    }
}
