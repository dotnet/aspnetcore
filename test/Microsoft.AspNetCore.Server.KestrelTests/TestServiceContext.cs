// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class TestServiceContext : ServiceContext
    {
        private RequestDelegate _app;

        public TestServiceContext()
        {
            AppLifetime = new LifetimeNotImplemented();
            Log = new TestKestrelTrace();
            ThreadPool = new LoggingThreadPool(Log);
            DateHeaderValueManager = new TestDateHeaderValueManager();
            HttpComponentFactory = new HttpComponentFactory();
        }

        public RequestDelegate App
        {
            get
            {
                return _app;
            }
            set
            {
                _app = value;
                FrameFactory = (connectionContext, remoteEP, localEP, prepareRequest) =>
                {
                    return new Frame<HttpContext>(new DummyApplication(_app), connectionContext, remoteEP, localEP, prepareRequest);
                };
            }
        }
    }
}
