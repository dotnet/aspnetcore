// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class TestServiceContext : ServiceContext
    {
        private RequestDelegate _app;

        public TestServiceContext()
        {
            AppLifetime = new LifetimeNotImplemented();
            Log = new TestKestrelTrace();
            DateHeaderValueManager = new TestDateHeaderValueManager();
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
