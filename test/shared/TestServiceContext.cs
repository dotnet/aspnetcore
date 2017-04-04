// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Testing
{
    public class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            var logger = new TestApplicationErrorLogger();

            Log = new TestKestrelTrace(logger);
            ThreadPool = new LoggingThreadPool(Log);
            DateHeaderValueManager = new DateHeaderValueManager(systemClock: new MockSystemClock());
            DateHeaderValue = DateHeaderValueManager.GetDateHeaderValues().String;
            HttpParserFactory = frame => new KestrelHttpParser(frame.ServiceContext.Log);
            ServerOptions = new KestrelServerOptions
            {
                AddServerHeader = false
            };

            TransportContext = new LibuvTransportContext
            {
                AppLifetime = new LifetimeNotImplemented(),
                Log = new LibuvTrace(logger),
                Options = new LibuvTransportOptions
                {
                    ThreadCount = 1,
                    ShutdownTimeout = TimeSpan.FromSeconds(5)
                }
            };
        }

        public string DateHeaderValue { get; }

        public LibuvTransportContext TransportContext { get; }
    }
}
