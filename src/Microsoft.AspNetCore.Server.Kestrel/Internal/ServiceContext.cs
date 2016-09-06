// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal
{
    public class ServiceContext
    {
        public IApplicationLifetime AppLifetime { get; set; }

        public IKestrelTrace Log { get; set; }

        public IThreadPool ThreadPool { get; set; }

        public Func<ConnectionContext, Frame> FrameFactory { get; set; }

        public DateHeaderValueManager DateHeaderValueManager { get; set; }

        public KestrelServerOptions ServerOptions { get; set; }
    }
}
