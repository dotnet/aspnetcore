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

namespace Microsoft.AspNetCore.Testing
{
    public class TestServiceContext : ServiceContext
    {
        private RequestDelegate _app;

        public TestServiceContext()
        {
            Log = new TestKestrelTrace();
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
                Log = Log,
                Options = new LibuvTransportOptions
                {
                    ThreadCount = 1,
                    ShutdownTimeout = TimeSpan.FromSeconds(5)
                }
            };
        }

        public string DateHeaderValue { get; }

        public LibuvTransportContext TransportContext { get; }

        public RequestDelegate App
        {
            get
            {
                return _app;
            }
            set
            {
                TransportContext.ConnectionHandler = new ConnectionHandler<HttpContext>(this, new DummyApplication(value));
                _app = value;
            }
        }
    }
}
