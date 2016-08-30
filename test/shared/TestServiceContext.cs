// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Testing
{
    public class TestServiceContext : ServiceContext
    {
        private RequestDelegate _app;

        public TestServiceContext()
        {
            AppLifetime = new LifetimeNotImplemented();
            Log = new TestKestrelTrace();
            ThreadPool = new LoggingThreadPool(Log);
            DateHeaderValueManager = new DateHeaderValueManager(systemClock: new MockSystemClock());
            DateHeaderValue = DateHeaderValueManager.GetDateHeaderValues().String;
            ServerOptions = new KestrelServerOptions { AddServerHeader = false };
            ServerOptions.ShutdownTimeout = TimeSpan.FromSeconds(5);
        }

        public TestServiceContext(IConnectionFilter filter)
            : this()
        {
            ServerOptions.ConnectionFilter = filter;
        }

        public string DateHeaderValue { get; }

        public RequestDelegate App
        {
            get
            {
                return _app;
            }
            set
            {
                _app = value;
                FrameFactory = connectionContext =>
                {
                    return new Frame<HttpContext>(new DummyApplication(_app), connectionContext);
                };
            }
        }
    }
}
